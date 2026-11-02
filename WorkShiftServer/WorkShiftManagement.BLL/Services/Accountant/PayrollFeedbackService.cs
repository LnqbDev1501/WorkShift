using WorkShiftManagement.BLL.Interfaces.Realtime;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using Issue = WorkShiftManagement.DAL.Entities.PayrollCorrectionRequest;
using RecalculateRequest = WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation.PayrollCorrectionRequest;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed partial class PayrollFeedbackService(IPayrollFeedbackRepo repo, IPayrollCalculationRepo calculation,
    TimeProvider clock, ILogger<PayrollFeedbackService> logger, INotificationRealtimePublisher realtimePublisher,
    IAdminRealtimePublisher? adminRealtimePublisher = null) : IPayrollFeedbackService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static string Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 2000
        ? value.Trim() : throw new ArgumentException("Content is required and must not exceed 2000 characters.");
    private async Task<User> Actor(Guid id, CancellationToken ct, params string[] roles)
    {
        var u = await calculation.GetActorAsync(id, ct);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE" || !roles.Contains(u.Role.RoleName)
            || u.Role.RoleName is "ACCOUNTANT" or "MANAGER" && u.BranchId == null)
            throw new UnauthorizedAccessException("User is not authorized to manage payroll.");
        return u;
    }
    private async Task<PayrollReviewData> Data(User u, int? periodId, CancellationToken ct)
    {
        if (periodId is null or <= 0) throw new KeyNotFoundException("Payroll data not found.");
        var d = await repo.LoadAsync(periodId.Value, ct) ?? throw new KeyNotFoundException("Payroll period not found.");
        if (u.Role.RoleName is "ACCOUNTANT" or "MANAGER" && d.Period.BranchId != u.BranchId)
            throw new UnauthorizedAccessException("Payroll period does not belong to your assigned branch.");
        return d;
    }
    private static void Editable(PayrollReviewData d)
    { if (!PayrollPreviewPolicy.Mutable(d.Period)) throw new InvalidOperationException("Period no longer permits modifications or responses to Payroll Preview."); }
    private static Payroll Current(PayrollReviewData d, int id)
        => PayrollPreviewPolicy.Current(d).SingleOrDefault(p => p.PayrollId == id) ?? throw new InvalidOperationException("Only the current active Payroll version can be processed.");
    private static PayrollFeedbackRequest Review(PayrollReviewData d, int id, User u)
    {
        var row = d.Reviews.SingleOrDefault(f => f.FeedbackId == id) ?? throw new KeyNotFoundException("Review round not found.");
        if (u.Role.RoleName == "STAFF" && row.EmployeeId != u.UserId) throw new UnauthorizedAccessException("Staff can only view their own Payroll Preview.");
        return row;
    }
    private void Respondable(PayrollReviewData d, PayrollFeedbackRequest f)
    {
        Editable(d); Current(d, f.PayrollId);
        if (f.ReviewStatus != "PENDING" || !PayrollPreviewPolicy.Leaf(d, f) || !f.ReviewDeadlineAt.HasValue
            || Now < f.CreatedAt || Now >= f.ReviewDeadlineAt || PayrollPreviewPolicy.Root(d, f).ClosedAt != null)
            throw new InvalidOperationException("Review round has already been responded to, expired, or superseded.");
    }
    private void Expire(PayrollReviewData d)
    {
        if (!PayrollPreviewPolicy.Mutable(d.Period)) return;
        foreach (var f in d.Reviews.Where(f => f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt <= Now && PayrollPreviewPolicy.Leaf(d, f)).ToList())
        {
            // Never expire an accepted/submitted feedback while Manager/Accountant is still handling it.
            if (d.Corrections.Any(r => r.Payroll.UserId == f.EmployeeId && r.Status is "OPEN" or "RESPONDED")) continue;
            f.ReviewStatus = "EXPIRED"; f.UpdatedAt = Now;
            var root = PayrollPreviewPolicy.Root(d, f); root.ClosedAt = Now; root.ClosureReason = "REVIEW_EXPIRED"; root.UpdatedAt = Now;
            Audit(null, "EXPIRE_PAYROLL_PREVIEW", d, new { f.FeedbackId, f.PayrollId });
        }
        foreach (var r in d.Corrections.Where(r => r.Status == "OPEN" && r.ResponseDeadlineAt <= Now).ToList())
        {
            r.Status = "EXPIRED"; r.ExpiredAt = Now; r.UpdatedAt = Now;
            Audit(null, "EXPIRE_PAYROLL_CORRECTION_REQUEST", d, new { r.CorrectionRequestId, r.PayrollId });
        }
    }
    private void Audit(Guid? actor, string action, PayrollReviewData d, object payload)
        => repo.Audit(actor, action, "payroll_periods", d.Period.PayrollPeriodId, payload, Now);
    private void Notify(PayrollReviewData d, string role, Guid? user, string title, string message, Guid? sender = null)
    {
        repo.Audit(sender, "PAYROLL_SYSTEM_EVENT", "payroll_periods", d.Period.PayrollPeriodId,
            new PayrollSystemMessage(d.Period.PayrollPeriodId, d.Period.BranchId, role, user, title, message), Now);
        repo.NotifyInApp(d.Period.PayrollPeriodId, d.Period.BranchId, role, user, title, message, Now, sender);
    }
    private async Task DispatchRealtimeNotificationsAsync()
    {
        var pendings = repo.PendingNotifications.ToList();
        repo.ClearPendingNotifications();
        foreach (var pending in pendings)
        {
            if (pending.Notification.NotificationId <= 0) continue;
            var payload = new RealtimeNotificationResponse
            {
                NotificationId = pending.Notification.NotificationId,
                Title = pending.Notification.Title,
                Message = pending.Notification.Content,
                CreatedAt = pending.Notification.CreatedAt
            };
            foreach (var recipientId in pending.RecipientUserIds)
            {
                try
                {
                    await realtimePublisher.PublishAsync(recipientId, payload);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to publish realtime notification {NotificationId} to user {UserId}.", payload.NotificationId, recipientId);
                }
            }
        }
    }
    private PayrollFeedbackRequest NewRound(PayrollReviewData d, Payroll p, Guid actor, PayrollFeedbackRequest? previous, string? note)
    {
        if (previous == null)
        {
            if (!d.Period.InitialFeedbackDeadlineAt.HasValue || Now >= d.Period.InitialFeedbackDeadlineAt)
                throw new InvalidOperationException("Feedback window for this preview round has expired.");
        }
        else
        {
            if (!PayrollPreviewPolicy.Leaf(d, previous))
                throw new InvalidOperationException("Review case is already closed or superseded by a subsequent round.");
            previous.ReviewStatus = "SUPERSEDED"; previous.UpdatedAt = Now;
        }
        var row = new PayrollFeedbackRequest {
            Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId, PreviousFeedbackId = previous?.FeedbackId,
            RoundNo = (previous?.RoundNo ?? 0) + 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now,
            ReviewDeadlineAt = previous == null ? d.Period.InitialFeedbackDeadlineAt : (previous.ReviewDeadlineAt.HasValue && Now < previous.ReviewDeadlineAt.Value ? previous.ReviewDeadlineAt.Value : PayrollPreviewPolicy.StandardDeadline(Now)),
            ReviewStatus = "PENDING", ResultNote = note
        };
        repo.Add(row); d.Reviews.Add(row); Notify(d, "STAFF", p.UserId, "Payroll Preview Available for Review", $"Payroll preview for period {d.Period.PeriodCode} (Version {p.VersionNo}) is ready for your review.", actor); return row;
    }
    private Issue NewIssue(PayrollReviewData d, Payroll p, Guid actor, string reason, PayrollFeedbackRequest? feedback, Issue? previous = null)
    {
        if (d.Corrections.Any(r => r.Payroll.UserId == p.UserId && r.Status is "OPEN" or "RESPONDED"))
            throw new InvalidOperationException("Staff member has a pending Correction Request in progress.");
        var deadline = (previous != null && Now < previous.ResponseDeadlineAt)
            ? previous.ResponseDeadlineAt
            : PayrollPreviewPolicy.StandardDeadline(Now);
        var row = new Issue { Payroll = p, PayrollId = p.PayrollId, FeedbackId = feedback?.FeedbackId, PreviousRequestId = previous?.CorrectionRequestId,
            Reason = reason, RequestedBy = actor, Status = "OPEN", CreatedAt = Now, UpdatedAt = Now,
            ResponseDeadlineAt = deadline };
        repo.Add(row); d.Corrections.Add(row); p.Status = "REJECTED"; p.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED"; d.Period.UpdatedAt = Now;
        var notifTitle = previous != null ? "Payroll Correction Request Returned" : "Payroll Correction Request Created";
        var notifBody = previous != null
            ? $"{d.Period.PeriodCode}: Correction request #{previous.CorrectionRequestId} returned. Reason: {reason}"
            : $"{d.Period.PeriodCode}: Payroll #{p.PayrollId} for {p.EmployeeFullNameSnapshot} requires correction or explanation. Reason: {reason}";
        Notify(d, "MANAGER", null, notifTitle, notifBody, actor); return row;
    }
    private static void RefreshPeriod(PayrollReviewData d, DateTime now)
    {
        var leafCorrections = d.Corrections.Where(r => PayrollPreviewPolicy.Leaf(d, r)).ToList();
        var leafReviews = d.Reviews.Where(f => PayrollPreviewPolicy.Leaf(d, f)).ToList();
        if (d.Period.Status == "REVISION_REQUIRED"
            && leafCorrections.All(r => r.Status == "COMPLETED")
            && !leafReviews.Any(r => r.ReviewStatus == "FEEDBACK_PENDING")
            && PayrollPreviewPolicy.Current(d).All(p => p.Status == "CALCULATED"))
        {
            d.Period.Status = "CALCULATED";
            d.Period.UpdatedAt = now;
        }
    }
    public async Task<List<PayrollFeedbackResponse>> SendPreviewsAsync(Guid actor, int periodId, SendPayrollPreviewsRequest? request, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, periodId, ct); Editable(d); Expire(d);
        var current = PayrollPreviewPolicy.Current(d);
        if (current.Count == 0 || current.Any(p => p.Status != "CALCULATED") || d.Corrections.Any(r => r.Status is "OPEN" or "RESPONDED"))
            throw new InvalidOperationException("All errors must be resolved and all payroll records calculated before dispatching Previews.");
        await ValidateSources(d, actor, ct, requireResolvedAdjustments: false);
        // Establish the shared initial cutoff only when the first Staff preview is actually sent.
        bool hasDispatchedReviews = d.Reviews.Any(f => f.PreviousFeedbackId == null);
        if (!hasDispatchedReviews)
        {
            if (request?.InitialFeedbackDeadlineAt.HasValue == true)
            {
                var deadlineUtc = request.InitialFeedbackDeadlineAt.Value.UtcDateTime;
                if (deadlineUtc <= Now)
                    throw new ArgumentException("Review deadline must be later than the dispatch time.");
                d.Period.InitialFeedbackDeadlineAt = deadlineUtc;
            }
            else
            {
                d.Period.InitialFeedbackDeadlineAt = PayrollPreviewPolicy.StandardDeadline(Now);
            }
        }
        else if (!d.Period.InitialFeedbackDeadlineAt.HasValue)
        {
            d.Period.InitialFeedbackDeadlineAt = PayrollPreviewPolicy.StandardDeadline(Now);
        }
        var sent = new List<PayrollFeedbackRequest>();
        foreach (var p in current)
        {
            var leaf = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
            if (leaf != null) { if (leaf.PayrollId != p.PayrollId) throw new InvalidOperationException("New revision must be dispatched via existing correction case."); sent.Add(leaf); continue; }
            sent.Add(NewRound(d, p, actor, null, null));
        }
        await calculation.SaveAsync(ct); Audit(actor, "SEND_PAYROLL_PREVIEWS", d, new { feedbackIds = sent.Select(f => f.FeedbackId), d.Period.InitialFeedbackDeadlineAt });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return sent.Select(f => Map(d, f, u)).ToList();
    }
    public async Task<PayrollFeedbackResponse> ConfirmAsync(Guid actor, int id, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "STAFF"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); var f = Review(d, id, u); Respondable(d, f);
        f.ReviewStatus = "CONFIRMED"; f.StaffRespondedAt = Now; f.UpdatedAt = Now;
        var root = PayrollPreviewPolicy.Root(d, f); root.ClosedAt = Now; root.ClosureReason = "STAFF_CONFIRMED"; root.UpdatedAt = Now;
        RefreshPeriod(d, Now); Audit(actor, "CONFIRM_PAYROLL_PREVIEW", d, new { id, f.PayrollId });
        Notify(d, "ACCOUNTANT", f.SentBy, "Payroll Preview Confirmed", $"Staff {u.FullName} has confirmed their payroll preview for period {d.Period.PeriodCode}.", actor);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return Map(d, f, u);
    }
    public async Task<PayrollFeedbackResponse> SubmitAsync(Guid actor, int id, SubmitPayrollFeedbackRequest request, CancellationToken ct)
    {
        var text = Text(request.Description); var u = await Actor(actor, ct, "STAFF"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); var f = Review(d, id, u); Respondable(d, f);
        if (d.Corrections.Any(r => r.Payroll.UserId == actor && r.Status is "OPEN" or "RESPONDED")) throw new InvalidOperationException("A correction request is already being processed for this staff member.");
        f.Description = text; f.Status = "SUBMITTED"; f.ReviewStatus = "FEEDBACK_PENDING"; f.StaffRespondedAt = Now; f.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED"; d.Period.UpdatedAt = Now;
        Notify(d, "ACCOUNTANT", f.SentBy, "New Payroll Feedback Submitted", $"Staff {u.FullName} submitted feedback for period {d.Period.PeriodCode} (Feedback #{id}).", actor);
        Audit(actor, "SUBMIT_PAYROLL_PREVIEW_FEEDBACK", d, new { id, f.PayrollId }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return Map(d, f, u);
    }
    public async Task<PayrollFeedbackResponse> ApproveAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
    {
        var note = Text(request.ResolutionNote); var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); Editable(d); var f = Review(d, id, u); var p = Current(d, f.PayrollId);
        if (f.Status != "SUBMITTED" || f.ReviewStatus != "FEEDBACK_PENDING" || !PayrollPreviewPolicy.Leaf(d, f)) throw new InvalidOperationException("Feedback is no longer pending Accountant review.");
        f.Status = "APPROVED"; f.ProcessedBy = actor; f.ProcessedAt = Now; f.ResolutionNote = note; f.UpdatedAt = Now;
        NewIssue(d, p, actor, f.Description!, f);
        Notify(d, "STAFF", f.EmployeeId, "Payroll Feedback Approved", $"Your feedback for period {d.Period.PeriodCode} has been approved and forwarded to the branch manager for correction.", actor);
        await calculation.SaveAsync(ct); Audit(actor, "APPROVE_PAYROLL_FEEDBACK", d, new { id, f.Status, note });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return Map(d, f, u);
    }
    public async Task<PayrollFeedbackResponse> ExplainAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
    {
        var note = Text(request.ResolutionNote); var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); Editable(d); var f = Review(d, id, u);
        if (f.Status != "SUBMITTED" || f.ReviewStatus != "FEEDBACK_PENDING" || !PayrollPreviewPolicy.Leaf(d, f)) throw new InvalidOperationException("Feedback is no longer pending Accountant review.");
        f.Status = "EXPLAINED"; f.ProcessedBy = actor; f.ProcessedAt = Now; f.ResolutionNote = note; f.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED"; d.Period.UpdatedAt = Now;
        Notify(d, "STAFF", f.EmployeeId, "Payroll Feedback Explanation", $"Accountant provided an explanation for your feedback in period {d.Period.PeriodCode}: {note}", actor);
        await calculation.SaveAsync(ct); Audit(actor, "EXPLAIN_PAYROLL_FEEDBACK", d, new { id, f.Status, note });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return Map(d, f, u);
    }
    public Task<PayrollFeedbackResponse> RejectAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
        => ExplainAsync(actor, id, request, ct);
    public async Task<PayrollIssueResponse> CreateIssueAsync(Guid actor, int payrollId, CreatePayrollIssueRequest request, CancellationToken ct)
    {
        var reason = Text(request.Reason); var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForPayrollAsync(payrollId, ct), ct); Editable(d); Expire(d); var p = Current(d, payrollId);
        if (p.Status != "CALCULATED" || d.Period.InitialFeedbackDeadlineAt.HasValue && Now >= d.Period.InitialFeedbackDeadlineAt)
            throw new InvalidOperationException("Initial feedback window has expired or Payroll is not ready.");
        var f = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
        if (f != null)
        {
            if (f.ReviewStatus is "CONFIRMED" or "EXPIRED" && PayrollPreviewPolicy.Root(d, f).ClosedAt.HasValue)
            {
                // This is a NEW issue opened by Accountant before the common cutoff, not a reopening of a closed case.
                f = new PayrollFeedbackRequest { Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId,
                    RoundNo = 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now, ReviewStatus = "SUPERSEDED",
                    ReviewDeadlineAt = d.Period.InitialFeedbackDeadlineAt, ResultNote = "Accountant is reviewing a new issue: " + reason };
                repo.Add(f); d.Reviews.Add(f); await calculation.SaveAsync(ct);
            }
            else
            {
                if (f.ReviewStatus != "PENDING" || PayrollPreviewPolicy.Root(d, f).ClosedAt.HasValue) throw new InvalidOperationException("An active feedback case already exists; please resolve the existing case.");
                f.ReviewStatus = "SUPERSEDED"; f.UpdatedAt = Now;
            }
        }
        var issue = NewIssue(d, p, actor, reason, f); await calculation.SaveAsync(ct);
        Audit(actor, "CREATE_PAYROLL_CORRECTION_REQUEST", d, new { issue.CorrectionRequestId, payrollId, reason }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return MapIssue(d, issue);
    }
    public async Task<PayrollIssueResponse> RespondAsync(Guid actor, int id, RespondPayrollIssueRequest request, CancellationToken ct)
    {
        var note = Text(request.Note); if (request.Action is not ("CORRECT" or "EXPLAIN")) throw new ArgumentException("Manager must select either CORRECT or EXPLAIN.");
        var u = await Actor(actor, ct, "MANAGER"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(id, ct), ct); Editable(d);
        var r = d.Corrections.Single(x => x.CorrectionRequestId == id); var p = Current(d, r.PayrollId);
        if (!PayrollPreviewPolicy.Leaf(d, r)) throw new InvalidOperationException("Only the latest request can be responded to.");
        if (r.Status != "OPEN") throw new InvalidOperationException("Request is no longer in OPEN status.");
        if (Now >= r.ResponseDeadlineAt)
        {
            r.Status = "EXPIRED";
            r.ExpiredAt = Now;
            r.UpdatedAt = Now;
            Audit(actor, "EXPIRE_PAYROLL_CORRECTION_REQUEST", d, new { r.CorrectionRequestId, r.PayrollId });
            await calculation.SaveAsync(ct);
            await tx.CommitAsync(ct);
            throw new InvalidOperationException("Request feedback deadline has expired.");
        }
        if (request.Action == "CORRECT")
        {
            var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), p.UserId, actor, Now, true);
            if (!PayrollPreviewPolicy.Changed(p, rebuilt)) throw new InvalidOperationException("No valid source changes detected. Cannot select CORRECT with notes alone; please select 'EXPLAIN (Explanation Only)' if providing explanation.");
            repo.Audit(actor, "PAYROLL_CORRECTION_SOURCE_IMPACT", "payroll_correction_requests", id,
                new CorrectionImpactEvidence { SharedChangeKeys = PayrollSourceImpactPolicy.SharedChanges(p, rebuilt) }, Now);
        }
        r.Status = "RESPONDED"; r.ResponseAction = request.Action; r.ResponseNote = note; r.RespondedBy = actor; r.RespondedAt = Now; r.UpdatedAt = Now;
        var responseTitle = request.Action == "CORRECT"
            ? "Correction Request Responded: Timesheet Corrected"
            : "Correction Request Responded: Explanation Provided";
        var responseBody = request.Action == "CORRECT"
            ? $"Manager corrected timesheet source for request #{id} in period {d.Period.PeriodCode}. Note: {note}"
            : $"Manager provided an explanation for request #{id} in period {d.Period.PeriodCode}. Note: {note}";
        Notify(d, "ACCOUNTANT", r.RequestedBy, responseTitle, responseBody, actor);
        Audit(actor, "RESPOND_PAYROLL_CORRECTION_REQUEST", d, new { id, r.ResponseAction, note }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return MapIssue(d, r);
    }
    private void Complete(PayrollReviewData d, Issue r, Payroll p, Guid actor, string note)
    {
        r.Status = "COMPLETED"; r.ReviewedBy = actor; r.ReviewedAt = Now; r.ReviewNote = note; r.ResolvedPayroll = p;
        r.ResolvedPayrollId = p.PayrollId; r.CompletedAt = Now; r.UpdatedAt = Now;
        RefreshPeriod(d, Now);
    }
    public async Task<PayrollIssueResponse> ReviewIssueAsync(Guid actor, int id, ReviewPayrollIssueRequest request, CancellationToken ct)
    {
        var note = Text(request.Note); if (request.Decision is not ("ACCEPT_EXPLANATION" or "RETURN")) throw new ArgumentException("Please select ACCEPT_EXPLANATION or RETURN.");
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(id, ct), ct); Editable(d); var r = d.Corrections.Single(x => x.CorrectionRequestId == id);
        if (!PayrollPreviewPolicy.Leaf(d, r)) throw new InvalidOperationException("Only the latest request can be processed.");
        if (Candidate(d, r) != null) throw new InvalidOperationException("Saved revision already exists awaiting verification. Preview/recalculate if sources changed, then complete/dispatch revision.");
        var p = Current(d, r.PayrollId);
        if (r.Status != "RESPONDED") throw new InvalidOperationException("Manager has not yet responded to this request.");
        if (request.Decision == "RETURN")
        {
            r.Status = "RETURNED"; r.ReviewedBy = actor; r.ReviewedAt = Now; r.ReviewNote = note; r.UpdatedAt = Now;
            NewIssue(d, p, actor, note, r.FeedbackId.HasValue ? d.Reviews.Single(f => f.FeedbackId == r.FeedbackId) : null, r);
        }
        else
        {
            if (r.ResponseAction != "EXPLAIN") throw new InvalidOperationException("CORRECT action requires recalculation; cannot keep existing Payroll via explanation acceptance.");
            var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), p.UserId, actor, Now, true);
            if (PayrollPreviewPolicy.Changed(p, rebuilt)) throw new InvalidOperationException("Sources have changed; must request CORRECT and calculate a new version.");
            p.Status = "CALCULATED"; p.UpdatedAt = Now; Complete(d, r, p, actor, note);
        }
        await calculation.SaveAsync(ct); Audit(actor, "REVIEW_PAYROLL_CORRECTION_REQUEST", d, new { id, request.Decision, note }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return MapIssue(d, r);
    }
    public async Task<PayrollCalculationResponse> RecalculateAsync(Guid actor, int payrollId, RecalculateRequest request, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForPayrollAsync(payrollId, ct), ct); Editable(d);
        var r = d.Corrections.SingleOrDefault(r => r.CorrectionRequestId == request.CorrectionRequestId)
            ?? throw new KeyNotFoundException("Correction Request not found in this payroll period.");
        if (!PayrollPreviewPolicy.Leaf(d, r)) throw new InvalidOperationException("Recalculation is only permitted for the latest correction request.");
        var original = d.Payrolls.Single(p => p.PayrollId == r.PayrollId);
        var old = PayrollPreviewPolicy.Current(d).Single(p => p.UserId == original.UserId);
        if (payrollId != original.PayrollId && payrollId != old.PayrollId) throw new InvalidOperationException("Payroll does not belong to the current revision of this request.");
        RequireCorrectResponse(d, r, old);
        var next = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), old.UserId, actor, Now, true);
        if (!PayrollPreviewPolicy.Changed(old, next))
        {
            if (old.PayrollId != original.PayrollId) { await tx.CommitAsync(ct); return PayrollCalculationService.Map(old); }
            throw new InvalidOperationException("Sources and results have not changed; new version cannot be created solely to update notes.");
        }
        // Register only shared-source changes captured at the Manager's CORRECT response.
        // Other stale payrolls remain listed as blockers, never silently included in this case.
        await RegisterRelated(d, r, actor, ct);
        old.Status = "REPLACED"; old.ReplacedAt = Now; old.UpdatedAt = Now;
        await calculation.SaveAsync(ct);
        next.PayrollPeriod = d.Period; next.PreviousPayrollId = old.PayrollId; next.VersionNo = checked(old.VersionNo + 1); next.CorrectionReason = r.Reason;
        PayrollApprovalService.ValidateTotals(next);
        if (next.TotalDeduction != next.TotalPenalty + next.AttendanceDeduction + next.CarryForwardDeduction - next.DeferredDeductionAmount)
            throw new InvalidOperationException($"Deduction figures for Payroll V{next.VersionNo} are invalid: Total deduction ({next.TotalDeduction:N0} VND) != Penalty ({next.TotalPenalty:N0} VND) + Attendance deduction ({next.AttendanceDeduction:N0} VND) + Carry forward debt ({next.CarryForwardDeduction:N0} VND) - Deferred deduction ({next.DeferredDeductionAmount:N0} VND).");
        calculation.Add(next); await calculation.SaveAsync(ct); d.Payrolls.Add(next);
        // RESPONDED remains open. Do not set resolved_payroll_id/completed_at and do not send Staff yet.
        Audit(actor, "RECALCULATE_PAYROLL_AWAITING_ACCOUNTANT", d, new { oldPayrollId = old.PayrollId, newPayrollId = next.PayrollId, next.VersionNo, r.CorrectionRequestId });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return PayrollCalculationService.Map(next);
    }
    private async Task ValidateSources(PayrollReviewData d, Guid actor, CancellationToken ct, bool requireResolvedAdjustments = true)
    {
        var input = await calculation.GetInputsAsync(d.Period, ct);
        if (input.EmployeeIds.Count == 0) throw new InvalidOperationException("Payroll period has no eligible staff members to calculate.");
        var impact = AssessSources(d, input, actor, requireResolvedAdjustments);
        if (impact.HasBlockingChanges) throw new PayrollSourceChangedException(impact);
    }
    public async Task SubmitPeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct); var d = await Data(u, periodId, ct);
        if (d.Period.Status == "SUBMITTED") { await tx.CommitAsync(ct); return; }
        Editable(d); Expire(d); RefreshPeriod(d, Now); PayrollPreviewPolicy.RequireReviewed(d);
        if (d.Period.Status != "CALCULATED" || PayrollPreviewPolicy.Current(d).Any(p => p.Status != "CALCULATED")) throw new InvalidOperationException("Period and all associated Payrolls must be in CALCULATED status prior to submission.");
        await ValidateSources(d, actor, ct);
        foreach (var p in PayrollPreviewPolicy.Current(d)) { p.Status = "SUBMITTED"; p.SubmittedBy = actor; p.SubmittedAt = Now; p.UpdatedAt = Now; }
        d.Period.Status = "SUBMITTED"; d.Period.SubmittedBy = actor; d.Period.SubmittedAt = Now; d.Period.UpdatedAt = Now;
        Notify(d, "ADMIN", null, "Payroll Period Submitted for Approval", $"Payroll period {d.Period.PeriodCode} has been submitted for approval.", actor);
        Audit(actor, "SUBMIT_PAYROLL_PERIOD", d, new { periodId }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
    }
    public async Task ApprovePeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ADMIN"); await using var tx = await calculation.BeginCalculationAsync(ct); var d = await Data(u, periodId, ct);
        var current = PayrollPreviewPolicy.Current(d);
        if (d.Period.Status is "APPROVED" or "PAID" or "CLOSED" && current.Count > 0 && current.All(p => p.Payslip != null)) { await tx.CommitAsync(ct); return; }
        if (d.Period.Status != "SUBMITTED" || current.Any(p => p.Status != "SUBMITTED")) throw new InvalidOperationException("Only Admin can approve an entire period in SUBMITTED status.");
        PayrollPreviewPolicy.RequireReviewed(d); await ValidateSources(d, actor, ct);
        if (d.Payrolls.Any(p => p.Payslip != null)) throw new InvalidOperationException("Period contains inconsistent official payslips; duplicate publishing rejected.");
        foreach (var p in current)
        {
            p.Timesheet.Status = "LOCKED"; p.Timesheet.LockedAt = Now; p.Timesheet.LockedBy = actor; p.Timesheet.UpdatedAt = Now;
            p.Status = "APPROVED"; p.ApprovedBy = actor; p.ApprovedAt = Now; p.UpdatedAt = Now;
            repo.Add(PayslipPolicy.Create(p, Now));
            Notify(d, "STAFF", p.UserId, "Official Payslip Published", $"Your official payslip for period {d.Period.PeriodCode} has been published.", actor);
        }
        d.Period.Status = "APPROVED"; d.Period.ApprovedBy = actor; d.Period.ApprovedAt = Now; d.Period.UpdatedAt = Now;
        Notify(d, "ACCOUNTANT", null, "Payroll Period Approved", $"Admin has approved and finalized payroll period {d.Period.PeriodCode}.", actor);
        Audit(actor, "APPROVE_AND_PUBLISH_PAYROLL_PERIOD", d, new { periodId, payslipCount = current.Count }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
    }
    public async Task<PayrollIssueResponse> ResendIssueAsync(Guid actor, int correctionRequestId, ResendPayrollIssueRequest request, CancellationToken ct)
    {
        var reason = Text(request.Reason);
        var u = await Actor(actor, ct, "ACCOUNTANT");
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(correctionRequestId, ct), ct);
        Editable(d);
        var old = d.Corrections.Single(x => x.CorrectionRequestId == correctionRequestId);
        if (!PayrollPreviewPolicy.Leaf(d, old))
            throw new InvalidOperationException("Resubmission is only allowed from the latest correction request.");
        if (old.Status == "OPEN")
        {
            if (Now < old.ResponseDeadlineAt)
                throw new InvalidOperationException("Request has not expired yet; cannot resubmit prior to expiration.");
            old.Status = "EXPIRED";
            old.ExpiredAt = Now;
            old.UpdatedAt = Now;
        }
        else if (old.Status != "EXPIRED")
        {
            throw new InvalidOperationException("Can only resend requests that are EXPIRED.");
        }
        old.ExpiredAt ??= Now;
        old.UpdatedAt = Now;
        var p = Current(d, old.PayrollId);
        var deadline = PayrollPreviewPolicy.StandardDeadline(Now);
        var next = new Issue {
            Payroll = p,
            PayrollId = p.PayrollId,
            FeedbackId = old.FeedbackId,
            PreviousRequestId = old.CorrectionRequestId,
            Reason = reason,
            RequestedBy = actor,
            Status = "OPEN",
            CreatedAt = Now,
            UpdatedAt = Now,
            ResponseDeadlineAt = deadline
        };
        repo.Add(next);
        d.Corrections.Add(next);
        p.Status = "REJECTED";
        p.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED";
        d.Period.UpdatedAt = Now;
        Audit(actor, "RESEND_PAYROLL_CORRECTION_REQUEST", d, new {
            oldRequestId = old.CorrectionRequestId,
            newRequestId = next.CorrectionRequestId,
            reason,
            oldDeadline = old.ResponseDeadlineAt,
            newDeadline = next.ResponseDeadlineAt,
            payrollId = p.PayrollId,
            payrollVersionNo = p.VersionNo
        });
        Notify(d, "MANAGER", null, "Payroll Correction Request Resent", $"{d.Period.PeriodCode}: Correction request #{next.CorrectionRequestId} has been resent. Reason: {reason}", actor);
        await calculation.SaveAsync(ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return MapIssue(d, next);
    }
    public async Task<PayrollFeedbackResponse> ResendPreviewAsync(Guid actor, int feedbackId, ResendPayrollPreviewRequest request, CancellationToken ct)
    {
        var reason = Text(request.Reason);
        var u = await Actor(actor, ct, "ACCOUNTANT");
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(feedbackId, ct), ct);
        Editable(d);
        var old = Review(d, feedbackId, u);
        if (!PayrollPreviewPolicy.Leaf(d, old))
            throw new InvalidOperationException("Can only redispatch from the employee's latest review round.");
        if (old.Status == "SUBMITTED")
            throw new InvalidOperationException("Feedback has not been resolved yet (Accountant must approve correction or provide explanation first).");
        if (d.Corrections.Any(r => r.Payroll.UserId == old.EmployeeId && r.Status is "OPEN" or "RESPONDED"))
            throw new InvalidOperationException("A correction request is already in progress for this employee.");
        var p = PayrollPreviewPolicy.Current(d).SingleOrDefault(p => p.UserId == old.EmployeeId)
            ?? throw new InvalidOperationException("Current payroll preview round not found.");
        if (p.Status != "CALCULATED")
            throw new InvalidOperationException("Current active Payroll version is not in CALCULATED status.");
        var root = PayrollPreviewPolicy.Root(d, old);
        root.ClosedAt = null;
        root.ClosureReason = null;
        root.UpdatedAt = Now;
        if (old.ReviewStatus != "EXPIRED")
        {
            old.ReviewStatus = "SUPERSEDED";
        }
        old.UpdatedAt = Now;
        var deadline = (old.ReviewDeadlineAt.HasValue && Now < old.ReviewDeadlineAt.Value)
            ? old.ReviewDeadlineAt.Value
            : PayrollPreviewPolicy.StandardDeadline(Now);
        var next = new PayrollFeedbackRequest {
            Payroll = p,
            PayrollId = p.PayrollId,
            EmployeeId = p.UserId,
            PreviousFeedbackId = old.FeedbackId,
            RoundNo = (old.RoundNo ?? 0) + 1,
            SentBy = actor,
            CreatedAt = Now,
            UpdatedAt = Now,
            ReviewDeadlineAt = deadline,
            ReviewStatus = "PENDING",
            ResultNote = reason
        };
        repo.Add(next);
        d.Reviews.Add(next);
        RefreshPeriod(d, Now);
        Audit(actor, "RESEND_PAYROLL_PREVIEW", d, new {
            oldFeedbackId = old.FeedbackId,
            newFeedbackId = next.FeedbackId,
            reason,
            oldDeadline = old.ReviewDeadlineAt,
            newDeadline = next.ReviewDeadlineAt,
            payrollId = p.PayrollId,
            payrollVersionNo = p.VersionNo
        });
        Notify(d, "STAFF", p.UserId, "Payroll Preview Resent for Review", $"{d.Period.PeriodCode}: Payroll preview round {next.RoundNo} has been resent. Reason: {reason}", actor);
        await calculation.SaveAsync(ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return Map(d, next, u);
    }
    public async Task ExpireDueAsync(CancellationToken ct)
    {
        foreach (var id in await repo.DueReviewPeriodsAsync(Now, ct))
        {
            try
            {
                await using var tx = await calculation.BeginCalculationAsync(ct);
                var data = await repo.LoadAsync(id, ct);
                if (data != null)
                {
                    Expire(data);
                    RefreshPeriod(data, Now);
                    await calculation.SaveAsync(ct);
                }
                await tx.CommitAsync(ct);
                if (data != null && adminRealtimePublisher != null)
                    await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                        new { resource = "PayrollReviews", action = "EXPIRED" });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not expire payroll reviews for period {Id}.", id);
            }
        }
    }
    public async Task<PagedResponse<PayrollFeedbackResponse>> ListAsync(Guid actor, PayrollFeedbackListRequest q, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT", "STAFF");
        if (q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue || q.Keyword?.Length > 200
            || q.UserId == Guid.Empty || q.PayrollPeriodId <= 0 || q.Status != null && q.Status is not ("SUBMITTED" or "APPROVED" or "REJECTED" or "EXPLAINED")
            || q.ReviewStatus != null && q.ReviewStatus is not ("PENDING" or "CONFIRMED" or "FEEDBACK_PENDING" or "SUPERSEDED" or "EXPIRED")) throw new ArgumentException("Invalid filter or pagination parameters.");
        if (u.Role.RoleName == "STAFF" && q.UserId.HasValue && q.UserId != actor) throw new UnauthorizedAccessException("Staff members can only query their own records.");
        var ids = await repo.ReviewPeriodIdsAsync(u.Role.RoleName == "ACCOUNTANT" ? u.BranchId : null, u.Role.RoleName == "STAFF" ? actor : null, ct);
        var rows = new List<PayrollFeedbackResponse>();
        foreach (var id in ids.Where(id => !q.PayrollPeriodId.HasValue || id == q.PayrollPeriodId))
        {
            var d = await Data(u, id, ct);
            rows.AddRange(d.Reviews.Where(f => (u.Role.RoleName != "STAFF" || f.EmployeeId == actor) && (!q.UserId.HasValue || f.EmployeeId == q.UserId))
                .Select(f => Map(d, f, u)));
        }
        var filtered = rows.Where(f => (q.Status == null || f.Status == q.Status) && (q.ReviewStatus == null || f.ReviewStatus == q.ReviewStatus)
            && (string.IsNullOrWhiteSpace(q.Keyword) || f.EmployeeName.Contains(q.Keyword.Trim(), StringComparison.OrdinalIgnoreCase) || (f.Description ?? "").Contains(q.Keyword.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.FeedbackId).ToList();
        return new() { PageNumber = q.Page, PageSize = q.PageSize, TotalItems = filtered.Count, Items = filtered.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList() };
    }
    public async Task<PayrollFeedbackDetailsResponse> GetAsync(Guid actor, int id, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT", "STAFF"); var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); var f = Review(d, id, u);
        var pending = (await calculation.GetInputsAsync(d.Period, ct)).Adjustments
            .Where(a => a.UserId == f.EmployeeId && !a.IsDeleted && a.ApprovalStatus == "PENDING")
            .Select(a => new PendingPayrollAdjustment { RewardPenaltyId = a.RewardPenaltyId, Type = a.AdjustmentType, Name = a.AdjustmentName, Amount = a.Amount }).ToList();
        return new() { Feedback = Map(d, f, u), OriginalPayroll = PayrollCalculationService.Map(f.Payroll), PendingSourceAdjustments = pending };
    }
    public async Task<List<PayrollIssueResponse>> IssuesAsync(Guid actor, int periodId, CancellationToken ct)
    { var u = await Actor(actor, ct, "ACCOUNTANT", "MANAGER"); var d = await Data(u, periodId, ct); return d.Corrections.Select(r => MapIssue(d, r)).ToList(); }
    public async Task<List<PayrollIssueResponse>> ListIssuesAsync(Guid actor, int? periodId, string? status, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT", "MANAGER", "ADMIN");
        var branchId = u.Role.RoleName is "MANAGER" or "ACCOUNTANT" ? u.BranchId : null;
        var issues = await repo.QueryIssuesAsync(branchId, periodId, status, ct);
        var prevIds = issues.Where(i => i.PreviousRequestId.HasValue).Select(i => i.PreviousRequestId!.Value).ToHashSet();
        return issues.Select(r => new PayrollIssueResponse
        {
            CorrectionRequestId = r.CorrectionRequestId,
            PayrollId = r.PayrollId,
            FeedbackId = r.FeedbackId,
            PreviousRequestId = r.PreviousRequestId,
            Reason = r.Reason,
            Status = r.Status,
            RequestedBy = r.RequestedBy,
            CreatedAt = r.CreatedAt,
            ResponseAction = r.ResponseAction,
            ResponseNote = r.ResponseNote,
            RespondedBy = r.RespondedBy,
            RespondedAt = r.RespondedAt,
            ReviewedBy = r.ReviewedBy,
            ReviewedAt = r.ReviewedAt,
            ReviewNote = r.ReviewNote,
            ResolvedPayrollId = r.ResolvedPayrollId,
            CompletedAt = r.CompletedAt,
            ResponseDeadlineAt = r.ResponseDeadlineAt,
            ExpiredAt = r.ExpiredAt,
            CanResend = !prevIds.Contains(r.CorrectionRequestId) && (r.Status == "EXPIRED" || (r.Status == "OPEN" && r.ResponseDeadlineAt <= Now)),
            CanRespond = !prevIds.Contains(r.CorrectionRequestId) && r.Status == "OPEN" && r.ResponseDeadlineAt > Now,
            PayrollPeriodId = r.Payroll.PayrollPeriodId,
            PeriodCode = r.Payroll.PayrollPeriod?.PeriodCode ?? "",
            ScheduleWeekId = r.Payroll.PayrollPeriod?.ScheduleWeekId,
            EmployeeName = r.Payroll.EmployeeFullNameSnapshot ?? ""
        }).ToList();
    }
    private PayrollFeedbackResponse Map(PayrollReviewData d, PayrollFeedbackRequest f, User u)
    {
        var expired = f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt <= Now;
        var root = PayrollPreviewPolicy.Root(d, f);
        var prev = f.PreviousFeedbackId.HasValue ? d.Reviews.FirstOrDefault(r => r.FeedbackId == f.PreviousFeedbackId.Value) : null;
        return new() { FeedbackId = f.FeedbackId, PayrollId = f.PayrollId, PayrollVersionNo = f.Payroll.VersionNo, PayrollPeriodId = d.Period.PayrollPeriodId,
            EmployeeId = f.EmployeeId, EmployeeName = f.Payroll.EmployeeFullNameSnapshot, PreviousFeedbackId = f.PreviousFeedbackId, RoundNo = f.RoundNo,
            ReviewStatus = expired ? "EXPIRED" : f.ReviewStatus, Status = f.Status, Description = f.Description, CreatedAt = f.CreatedAt, SentBy = f.SentBy,
            ReviewDeadlineAt = f.ReviewDeadlineAt, StaffRespondedAt = f.StaffRespondedAt, ProcessedAt = f.ProcessedAt, ProcessedBy = f.ProcessedBy,
            ResolutionNote = f.ResolutionNote, ResultNote = f.ResultNote, ClosedAt = root.ClosedAt, ClosureReason = root.ClosureReason,
            CanRespond = u.Role.RoleName == "STAFF" && f.EmployeeId == u.UserId && f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt.HasValue && f.CreatedAt <= Now && !expired && root.ClosedAt == null && PayrollPreviewPolicy.Mutable(d.Period) && PayrollPreviewPolicy.Leaf(d, f)
                && PayrollPreviewPolicy.Current(d).Any(p => p.PayrollId == f.PayrollId),
            PreviousResolutionNote = prev?.ResolutionNote,
            PreviousFeedbackDescription = prev?.Description,
            Corrections = d.Corrections.Where(r => r.FeedbackId == f.FeedbackId).Select(r => MapIssue(d, r)).ToList() };
    }
    private static PayrollIssueResponse Map(Issue r) => new() {
        CorrectionRequestId = r.CorrectionRequestId, PayrollId = r.PayrollId, FeedbackId = r.FeedbackId,
        PreviousRequestId = r.PreviousRequestId, Reason = r.Reason, Status = r.Status, RequestedBy = r.RequestedBy, CreatedAt = r.CreatedAt, ResponseAction = r.ResponseAction,
        ResponseNote = r.ResponseNote, RespondedBy = r.RespondedBy, RespondedAt = r.RespondedAt, ReviewedBy = r.ReviewedBy, ReviewedAt = r.ReviewedAt,
        ReviewNote = r.ReviewNote, ResolvedPayrollId = r.ResolvedPayrollId, CompletedAt = r.CompletedAt,
        ResponseDeadlineAt = r.ResponseDeadlineAt, ExpiredAt = r.ExpiredAt,
        CanResend = r.Status is "EXPIRED" or "OPEN",
        CanRespond = r.Status == "OPEN"
    };
}
