using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using Issue = WorkShiftManagement.DAL.Entities.PayrollCorrectionRequest;
using RecalculateRequest = WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation.PayrollCorrectionRequest;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollFeedbackService(IPayrollFeedbackRepo repo, IPayrollCalculationRepo calculation,
    TimeProvider clock, ILogger<PayrollFeedbackService> logger) : IPayrollFeedbackService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static string Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 2000
        ? value.Trim() : throw new ArgumentException("Nội dung bắt buộc, tối đa 2000 ký tự.");
    private async Task<User> Actor(Guid id, CancellationToken ct, params string[] roles)
    {
        var u = await calculation.GetActorAsync(id, ct);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE" || !roles.Contains(u.Role.RoleName)
            || u.Role.RoleName is "ACCOUNTANT" or "MANAGER" && u.BranchId == null)
            throw new UnauthorizedAccessException("Tài khoản không có quyền xử lý lương.");
        return u;
    }
    private async Task<PayrollReviewData> Data(User u, int? periodId, CancellationToken ct)
    {
        if (periodId is null or <= 0) throw new KeyNotFoundException("Không tìm thấy dữ liệu lương.");
        var d = await repo.LoadAsync(periodId.Value, ct) ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương.");
        if (u.Role.RoleName is "ACCOUNTANT" or "MANAGER" && d.Period.BranchId != u.BranchId)
            throw new UnauthorizedAccessException("Kỳ lương không thuộc chi nhánh được phân công.");
        return d;
    }
    private static void Editable(PayrollReviewData d)
    { if (!PayrollPreviewPolicy.Mutable(d.Period)) throw new InvalidOperationException("Kỳ không còn cho phép sửa hoặc phản hồi Payroll Preview."); }
    private static Payroll Current(PayrollReviewData d, int id)
        => PayrollPreviewPolicy.Current(d).SingleOrDefault(p => p.PayrollId == id) ?? throw new InvalidOperationException("Chỉ xử lý version Payroll hiện hành.");
    private static PayrollFeedbackRequest Review(PayrollReviewData d, int id, User u)
    {
        var row = d.Reviews.SingleOrDefault(f => f.FeedbackId == id) ?? throw new KeyNotFoundException("Không tìm thấy lượt review.");
        if (u.Role.RoleName == "STAFF" && row.EmployeeId != u.UserId) throw new UnauthorizedAccessException("Staff chỉ xem Payroll Preview của mình.");
        return row;
    }
    private void Respondable(PayrollReviewData d, PayrollFeedbackRequest f)
    {
        Editable(d); Current(d, f.PayrollId);
        if (f.ReviewStatus != "PENDING" || !PayrollPreviewPolicy.Leaf(d, f) || !f.ReviewDeadlineAt.HasValue
            || Now < f.CreatedAt || Now >= f.ReviewDeadlineAt || PayrollPreviewPolicy.Root(d, f).ClosedAt != null)
            throw new InvalidOperationException("Lượt review đã phản hồi, hết hạn hoặc được thay thế.");
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
    }
    private void Audit(Guid? actor, string action, PayrollReviewData d, object payload)
        => repo.Audit(actor, action, "payroll_periods", d.Period.PayrollPeriodId, payload, Now);
    private void Notify(PayrollReviewData d, string role, Guid? user, string title, string message)
        => repo.Audit(null, "PAYROLL_SYSTEM_EVENT", "payroll_periods", d.Period.PayrollPeriodId,
            new PayrollSystemMessage(d.Period.PayrollPeriodId, d.Period.BranchId, role, user, title, message), Now);
    private PayrollFeedbackRequest NewRound(PayrollReviewData d, Payroll p, Guid actor, PayrollFeedbackRequest? previous, string? note)
    {
        if (previous == null)
        {
            if (!d.Period.InitialFeedbackDeadlineAt.HasValue || Now >= d.Period.InitialFeedbackDeadlineAt)
                throw new InvalidOperationException("Đã hết hạn mở phản hồi ban đầu.");
        }
        else
        {
            if (PayrollPreviewPolicy.Root(d, previous).ClosedAt.HasValue || !PayrollPreviewPolicy.Leaf(d, previous))
                throw new InvalidOperationException("Case đã đóng hoặc đã có lượt tiếp theo.");
            previous.ReviewStatus = "SUPERSEDED"; previous.UpdatedAt = Now;
        }
        var row = new PayrollFeedbackRequest {
            Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId, PreviousFeedbackId = previous?.FeedbackId,
            RoundNo = (previous?.RoundNo ?? 0) + 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now,
            ReviewDeadlineAt = previous == null ? d.Period.InitialFeedbackDeadlineAt : PayrollPreviewPolicy.EndOfLocalDay(Now),
            ReviewStatus = "PENDING", ResultNote = note
        };
        repo.Add(row); d.Reviews.Add(row); Notify(d, "STAFF", p.UserId, "Payroll Preview cần kiểm tra", $"{d.Period.PeriodCode}: Payroll V{p.VersionNo}, lượt review {row.RoundNo}."); return row;
    }
    private Issue NewIssue(PayrollReviewData d, Payroll p, Guid actor, string reason, PayrollFeedbackRequest? feedback, Issue? previous = null)
    {
        if (d.Corrections.Any(r => r.Payroll.UserId == p.UserId && r.Status is "OPEN" or "RESPONDED"))
            throw new InvalidOperationException("Nhân viên đang có Correction Request chưa hoàn tất.");
        var row = new Issue { Payroll = p, PayrollId = p.PayrollId, FeedbackId = feedback?.FeedbackId, PreviousRequestId = previous?.CorrectionRequestId,
            Reason = reason, RequestedBy = actor, Status = "OPEN", CreatedAt = Now, UpdatedAt = Now };
        repo.Add(row); d.Corrections.Add(row); p.Status = "REJECTED"; p.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED"; d.Period.UpdatedAt = Now; Notify(d, "MANAGER", null, "Accountant yêu cầu kiểm tra nguồn lương", $"{d.Period.PeriodCode}: Payroll #{p.PayrollId} cần sửa nguồn hoặc giải trình."); return row;
    }
    private static void RefreshPeriod(PayrollReviewData d, DateTime now)
    {
        if (d.Period.Status == "REVISION_REQUIRED" && !d.Corrections.Any(r => r.Status is "OPEN" or "RESPONDED")
            && !d.Reviews.Any(r => r.ReviewStatus == "FEEDBACK_PENDING") && PayrollPreviewPolicy.Current(d).All(p => p.Status == "CALCULATED"))
        { d.Period.Status = "CALCULATED"; d.Period.UpdatedAt = now; }
    }
    public async Task<List<PayrollFeedbackResponse>> SendPreviewsAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, periodId, ct); Editable(d); Expire(d);
        var current = PayrollPreviewPolicy.Current(d);
        if (current.Count == 0 || current.Any(p => p.Status != "CALCULATED") || d.Corrections.Any(r => r.Status is "OPEN" or "RESPONDED"))
            throw new InvalidOperationException("Phải xử lý xong lỗi và tính đầy đủ Payroll trước khi gửi Preview.");
        await ValidateSources(d, actor, ct, requireResolvedAdjustments: false);
        // Establish the shared initial cutoff only when the first Staff preview is actually sent.
        d.Period.InitialFeedbackDeadlineAt ??= PayrollPreviewPolicy.EndOfLocalDay(Now);
        var sent = new List<PayrollFeedbackRequest>();
        foreach (var p in current)
        {
            var leaf = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
            if (leaf != null) { if (leaf.PayrollId != p.PayrollId) throw new InvalidOperationException("Bản mới phải gửi tiếp qua case correction hiện có."); sent.Add(leaf); continue; }
            sent.Add(NewRound(d, p, actor, null, null));
        }
        await calculation.SaveAsync(ct); Audit(actor, "SEND_PAYROLL_PREVIEWS", d, new { feedbackIds = sent.Select(f => f.FeedbackId), d.Period.InitialFeedbackDeadlineAt });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return sent.Select(f => Map(d, f, u)).ToList();
    }
    public async Task<PayrollFeedbackResponse> ConfirmAsync(Guid actor, int id, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "STAFF"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); var f = Review(d, id, u); Respondable(d, f);
        f.ReviewStatus = "CONFIRMED"; f.StaffRespondedAt = Now; f.UpdatedAt = Now;
        var root = PayrollPreviewPolicy.Root(d, f); root.ClosedAt = Now; root.ClosureReason = "STAFF_CONFIRMED"; root.UpdatedAt = Now;
        RefreshPeriod(d, Now); Audit(actor, "CONFIRM_PAYROLL_PREVIEW", d, new { id, f.PayrollId });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(d, f, u);
    }
    public async Task<PayrollFeedbackResponse> SubmitAsync(Guid actor, int id, SubmitPayrollFeedbackRequest request, CancellationToken ct)
    {
        var text = Text(request.Description); var u = await Actor(actor, ct, "STAFF"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); var f = Review(d, id, u); Respondable(d, f);
        if (d.Corrections.Any(r => r.Payroll.UserId == actor && r.Status is "OPEN" or "RESPONDED")) throw new InvalidOperationException("Đang xử lý yêu cầu của nhân viên này.");
        f.Description = text; f.Status = "SUBMITTED"; f.ReviewStatus = "FEEDBACK_PENDING"; f.StaffRespondedAt = Now; f.UpdatedAt = Now;
        d.Period.Status = "REVISION_REQUIRED"; d.Period.UpdatedAt = Now;
        Notify(d, "ACCOUNTANT", null, "Staff gửi phản hồi Payroll Preview", $"{d.Period.PeriodCode}: cần xử lý phản hồi #{id}.");
        Audit(actor, "SUBMIT_PAYROLL_PREVIEW_FEEDBACK", d, new { id, f.PayrollId }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(d, f, u);
    }
    private async Task<PayrollFeedbackResponse> Decide(Guid actor, int id, ReviewPayrollFeedbackRequest request, bool approve, CancellationToken ct)
    {
        var note = Text(request.ResolutionNote); var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForReviewAsync(id, ct), ct); Editable(d); var f = Review(d, id, u); var p = Current(d, f.PayrollId);
        if (f.Status != "SUBMITTED" || f.ReviewStatus != "FEEDBACK_PENDING" || !PayrollPreviewPolicy.Leaf(d, f)) throw new InvalidOperationException("Phản hồi không còn chờ Accountant xử lý.");
        f.Status = approve ? "APPROVED" : "REJECTED"; f.ProcessedBy = actor; f.ProcessedAt = Now; f.ResolutionNote = note; f.UpdatedAt = Now;
        if (approve) NewIssue(d, p, actor, f.Description!, f);
        else { NewRound(d, p, actor, f, note); RefreshPeriod(d, Now); }
        await calculation.SaveAsync(ct); Audit(actor, approve ? "APPROVE_PAYROLL_FEEDBACK" : "REJECT_PAYROLL_FEEDBACK", d, new { id, f.Status, note });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(d, f, u);
    }
    public Task<PayrollFeedbackResponse> ApproveAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Decide(actor, id, request, true, ct);
    public Task<PayrollFeedbackResponse> RejectAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Decide(actor, id, request, false, ct);
    public async Task<PayrollIssueResponse> CreateIssueAsync(Guid actor, int payrollId, CreatePayrollIssueRequest request, CancellationToken ct)
    {
        var reason = Text(request.Reason); var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForPayrollAsync(payrollId, ct), ct); Editable(d); Expire(d); var p = Current(d, payrollId);
        if (p.Status != "CALCULATED" || d.Period.InitialFeedbackDeadlineAt.HasValue && Now >= d.Period.InitialFeedbackDeadlineAt)
            throw new InvalidOperationException("Đã hết hạn mở vấn đề ban đầu hoặc Payroll chưa sẵn sàng.");
        var f = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
        if (f != null)
        {
            if (f.ReviewStatus is "CONFIRMED" or "EXPIRED" && PayrollPreviewPolicy.Root(d, f).ClosedAt.HasValue)
            {
                // This is a NEW issue opened by Accountant before the common cutoff, not a reopening of a closed case.
                f = new PayrollFeedbackRequest { Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId,
                    RoundNo = 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now, ReviewStatus = "SUPERSEDED",
                    ReviewDeadlineAt = d.Period.InitialFeedbackDeadlineAt, ResultNote = "Accountant đang kiểm tra vấn đề mới: " + reason };
                repo.Add(f); d.Reviews.Add(f); await calculation.SaveAsync(ct);
            }
            else
            {
                if (f.ReviewStatus != "PENDING" || PayrollPreviewPolicy.Root(d, f).ClosedAt.HasValue) throw new InvalidOperationException("Đang có feedback/case; phải xử lý case hiện có.");
                f.ReviewStatus = "SUPERSEDED"; f.UpdatedAt = Now;
            }
        }
        var issue = NewIssue(d, p, actor, reason, f); await calculation.SaveAsync(ct);
        Audit(actor, "CREATE_PAYROLL_CORRECTION_REQUEST", d, new { issue.CorrectionRequestId, payrollId, reason }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(issue);
    }
    public async Task<PayrollIssueResponse> RespondAsync(Guid actor, int id, RespondPayrollIssueRequest request, CancellationToken ct)
    {
        var note = Text(request.Note); if (request.Action is not ("CORRECT" or "EXPLAIN")) throw new ArgumentException("Manager chọn CORRECT hoặc EXPLAIN.");
        var u = await Actor(actor, ct, "MANAGER"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(id, ct), ct); Editable(d);
        var r = d.Corrections.Single(x => x.CorrectionRequestId == id); var p = Current(d, r.PayrollId);
        if (r.Status != "OPEN") throw new InvalidOperationException("Yêu cầu không còn OPEN.");
        if (request.Action == "CORRECT")
        {
            var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), p.UserId, actor, Now, true);
            if (!PayrollPreviewPolicy.Changed(p, rebuilt)) throw new InvalidOperationException("Chưa có thay đổi nguồn hợp lệ. Không thể chọn CORRECT chỉ bằng ghi chú hoặc nhắc duyệt.");
        }
        r.Status = "RESPONDED"; r.ResponseAction = request.Action; r.ResponseNote = note; r.RespondedBy = actor; r.RespondedAt = Now; r.UpdatedAt = Now;
        Notify(d, "ACCOUNTANT", null, "Manager đã phản hồi yêu cầu lương", $"Yêu cầu #{id}: {r.ResponseAction}.");
        Audit(actor, "RESPOND_PAYROLL_CORRECTION_REQUEST", d, new { id, r.ResponseAction, note }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(r);
    }
    private void Complete(PayrollReviewData d, Issue r, Payroll p, Guid actor, string note)
    {
        r.Status = "COMPLETED"; r.ReviewedBy = actor; r.ReviewedAt = Now; r.ReviewNote = note; r.ResolvedPayroll = p;
        r.ResolvedPayrollId = p.PayrollId; r.CompletedAt = Now; r.UpdatedAt = Now;
        if (r.FeedbackId.HasValue) NewRound(d, p, actor, d.Reviews.Single(f => f.FeedbackId == r.FeedbackId), note);
        RefreshPeriod(d, Now);
    }
    public async Task<PayrollIssueResponse> ReviewIssueAsync(Guid actor, int id, ReviewPayrollIssueRequest request, CancellationToken ct)
    {
        var note = Text(request.Note); if (request.Decision is not ("ACCEPT_EXPLANATION" or "RETURN")) throw new ArgumentException("Chọn ACCEPT_EXPLANATION hoặc RETURN.");
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(id, ct), ct); Editable(d); var r = d.Corrections.Single(x => x.CorrectionRequestId == id); var p = Current(d, r.PayrollId);
        if (r.Status != "RESPONDED") throw new InvalidOperationException("Manager chưa phản hồi yêu cầu này.");
        if (request.Decision == "RETURN")
        {
            r.Status = "RETURNED"; r.ReviewedBy = actor; r.ReviewedAt = Now; r.ReviewNote = note; r.UpdatedAt = Now;
            NewIssue(d, p, actor, note, r.FeedbackId.HasValue ? d.Reviews.Single(f => f.FeedbackId == r.FeedbackId) : null, r);
        }
        else
        {
            if (r.ResponseAction != "EXPLAIN") throw new InvalidOperationException("CORRECT cần tính lại, không thể giữ nguyên Payroll bằng chấp nhận giải trình.");
            var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), p.UserId, actor, Now, true);
            if (PayrollPreviewPolicy.Changed(p, rebuilt)) throw new InvalidOperationException("Nguồn đã thay đổi; phải yêu cầu CORRECT và tính version mới.");
            p.Status = "CALCULATED"; p.UpdatedAt = Now; Complete(d, r, p, actor, note);
        }
        await calculation.SaveAsync(ct); Audit(actor, "REVIEW_PAYROLL_CORRECTION_REQUEST", d, new { id, request.Decision, note }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return Map(r);
    }
    public async Task<PayrollCalculationResponse> RecalculateAsync(Guid actor, int payrollId, RecalculateRequest request, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForPayrollAsync(payrollId, ct), ct); Editable(d); var old = Current(d, payrollId);
        var r = d.Corrections.SingleOrDefault(r => r.CorrectionRequestId == request.CorrectionRequestId && r.PayrollId == payrollId);
        if (old.Status != "REJECTED" || r?.Status != "RESPONDED" || r.ResponseAction != "CORRECT")
            throw new InvalidOperationException("Cần Payroll REJECTED và yêu cầu Manager đã CORRECT, chờ Accountant tính lại.");
        var next = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), old.UserId, actor, Now, true);
        if (!PayrollPreviewPolicy.Changed(old, next)) throw new InvalidOperationException("Nguồn/kết quả không thay đổi; không tạo version chỉ để đổi ghi chú.");
        old.Status = "REPLACED"; old.ReplacedAt = Now; old.UpdatedAt = Now;
        await calculation.SaveAsync(ct); // Release a unique current-version slot before inserting the successor.
        next.PayrollPeriod = d.Period; next.PreviousPayrollId = old.PayrollId; next.VersionNo = checked(old.VersionNo + 1); next.CorrectionReason = r.Reason;
        calculation.Add(next); await calculation.SaveAsync(ct); d.Payrolls.Add(next); Complete(d, r, next, actor, r.ResponseNote!);
        Audit(actor, "RECALCULATE_PAYROLL_FROM_SOURCE", d, new { oldPayrollId = old.PayrollId, newPayrollId = next.PayrollId, next.VersionNo, r.CorrectionRequestId });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return PayrollCalculationService.Map(next);
    }
    private async Task ValidateSources(PayrollReviewData d, Guid actor, CancellationToken ct, bool requireResolvedAdjustments = true)
    {
        var input = await calculation.GetInputsAsync(d.Period, ct); var current = PayrollPreviewPolicy.Current(d);
        if (current.Count == 0 || !input.EmployeeIds.ToHashSet().SetEquals(current.Select(p => p.UserId))) throw new InvalidOperationException("Thiếu Payroll hiện hành cho toàn bộ nhân viên.");
        if (requireResolvedAdjustments && input.Adjustments.Any(a => !a.IsDeleted && a.ApprovalStatus == "PENDING")) throw new InvalidOperationException("Còn thưởng/phạt chờ Admin duyệt; cần xử lý trước khi gửi Staff/Submit/Approve.");
        foreach (var p in current)
        {
            PayrollApprovalService.ValidateTotals(p);
            var rebuilt = PayrollCalculationPolicy.Build(input, p.UserId, actor, Now, true);
            if (PayrollPreviewPolicy.Changed(p, rebuilt)) throw new InvalidOperationException($"{p.EmployeeFullNameSnapshot}: nguồn đã thay đổi, cần Correction Request và version mới.");
        }
    }
    public async Task SubmitPeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct); var d = await Data(u, periodId, ct);
        if (d.Period.Status == "SUBMITTED") { await tx.CommitAsync(ct); return; }
        Editable(d); Expire(d); RefreshPeriod(d, Now); PayrollPreviewPolicy.RequireReviewed(d);
        if (d.Period.Status != "CALCULATED" || PayrollPreviewPolicy.Current(d).Any(p => p.Status != "CALCULATED")) throw new InvalidOperationException("Kỳ và toàn bộ Payroll phải CALCULATED trước Submit.");
        await ValidateSources(d, actor, ct);
        foreach (var p in PayrollPreviewPolicy.Current(d)) { p.Status = "SUBMITTED"; p.SubmittedBy = actor; p.SubmittedAt = Now; p.UpdatedAt = Now; }
        d.Period.Status = "SUBMITTED"; d.Period.SubmittedBy = actor; d.Period.SubmittedAt = Now; d.Period.UpdatedAt = Now;
        Notify(d, "ADMIN", null, "Kỳ lương chờ duyệt", d.Period.PeriodCode);
        Audit(actor, "SUBMIT_PAYROLL_PERIOD", d, new { periodId }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ApprovePeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ADMIN"); await using var tx = await calculation.BeginCalculationAsync(ct); var d = await Data(u, periodId, ct);
        var current = PayrollPreviewPolicy.Current(d);
        if (d.Period.Status is "APPROVED" or "PAID" or "CLOSED" && current.Count > 0 && current.All(p => p.Payslip != null)) { await tx.CommitAsync(ct); return; }
        if (d.Period.Status != "SUBMITTED" || current.Any(p => p.Status != "SUBMITTED")) throw new InvalidOperationException("Chỉ Admin duyệt toàn kỳ SUBMITTED.");
        PayrollPreviewPolicy.RequireReviewed(d); await ValidateSources(d, actor, ct);
        if (d.Payrolls.Any(p => p.Payslip != null)) throw new InvalidOperationException("Kỳ đã có phiếu chính thức không nhất quán; không phát lại.");
        foreach (var p in current)
        {
            p.Timesheet.Status = "LOCKED"; p.Timesheet.LockedAt = Now; p.Timesheet.LockedBy = actor; p.Timesheet.UpdatedAt = Now;
            p.Status = "APPROVED"; p.ApprovedBy = actor; p.ApprovedAt = Now; p.UpdatedAt = Now;
            repo.Add(PayslipPolicy.Create(p, Now));
            Notify(d, "STAFF", p.UserId, "Phiếu lương chính thức đã phát hành", d.Period.PeriodCode);
        }
        d.Period.Status = "APPROVED"; d.Period.ApprovedBy = actor; d.Period.ApprovedAt = Now; d.Period.UpdatedAt = Now;
        Notify(d, "ACCOUNTANT", null, "Admin đã duyệt kỳ lương", d.Period.PeriodCode);
        Audit(actor, "APPROVE_AND_PUBLISH_PAYROLL_PERIOD", d, new { periodId, payslipCount = current.Count }); await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ExpireDueAsync(CancellationToken ct)
    {
        foreach (var id in await repo.DueReviewPeriodsAsync(Now, ct))
        {
            try { await using var tx = await calculation.BeginCalculationAsync(ct); var d = await repo.LoadAsync(id, ct); if (d != null) { Expire(d); RefreshPeriod(d, Now); await calculation.SaveAsync(ct); } await tx.CommitAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Could not expire payroll reviews for period {Id}.", id); }
        }
    }
    public async Task<PagedResponse<PayrollFeedbackResponse>> ListAsync(Guid actor, PayrollFeedbackListRequest q, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT", "STAFF");
        if (q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue || q.Keyword?.Length > 200
            || q.UserId == Guid.Empty || q.PayrollPeriodId <= 0 || q.Status != null && q.Status is not ("SUBMITTED" or "APPROVED" or "REJECTED")
            || q.ReviewStatus != null && q.ReviewStatus is not ("PENDING" or "CONFIRMED" or "FEEDBACK_PENDING" or "SUPERSEDED" or "EXPIRED")) throw new ArgumentException("Bộ lọc/phân trang không hợp lệ.");
        if (u.Role.RoleName == "STAFF" && q.UserId.HasValue && q.UserId != actor) throw new UnauthorizedAccessException("Staff chỉ tra cứu của mình.");
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
    { var u = await Actor(actor, ct, "ACCOUNTANT", "MANAGER"); return (await Data(u, periodId, ct)).Corrections.Select(Map).ToList(); }
    private PayrollFeedbackResponse Map(PayrollReviewData d, PayrollFeedbackRequest f, User u)
    {
        var expired = f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt <= Now;
        var root = PayrollPreviewPolicy.Root(d, f);
        return new() { FeedbackId = f.FeedbackId, PayrollId = f.PayrollId, PayrollVersionNo = f.Payroll.VersionNo, PayrollPeriodId = d.Period.PayrollPeriodId,
            EmployeeId = f.EmployeeId, EmployeeName = f.Payroll.EmployeeFullNameSnapshot, PreviousFeedbackId = f.PreviousFeedbackId, RoundNo = f.RoundNo,
            ReviewStatus = expired ? "EXPIRED" : f.ReviewStatus, Status = f.Status, Description = f.Description, CreatedAt = f.CreatedAt, SentBy = f.SentBy,
            ReviewDeadlineAt = f.ReviewDeadlineAt, StaffRespondedAt = f.StaffRespondedAt, ProcessedAt = f.ProcessedAt, ProcessedBy = f.ProcessedBy,
            ResolutionNote = f.ResolutionNote, ResultNote = f.ResultNote, ClosedAt = root.ClosedAt, ClosureReason = root.ClosureReason,
            CanRespond = u.Role.RoleName == "STAFF" && f.EmployeeId == u.UserId && f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt.HasValue && f.CreatedAt <= Now && !expired && root.ClosedAt == null && PayrollPreviewPolicy.Mutable(d.Period) && PayrollPreviewPolicy.Leaf(d, f)
                && PayrollPreviewPolicy.Current(d).Any(p => p.PayrollId == f.PayrollId),
            Corrections = d.Corrections.Where(r => r.FeedbackId == f.FeedbackId).Select(Map).ToList() };
    }
    private static PayrollIssueResponse Map(Issue r) => new() { CorrectionRequestId = r.CorrectionRequestId, PayrollId = r.PayrollId, FeedbackId = r.FeedbackId,
        PreviousRequestId = r.PreviousRequestId, Reason = r.Reason, Status = r.Status, RequestedBy = r.RequestedBy, CreatedAt = r.CreatedAt, ResponseAction = r.ResponseAction,
        ResponseNote = r.ResponseNote, RespondedBy = r.RespondedBy, RespondedAt = r.RespondedAt, ReviewedBy = r.ReviewedBy, ReviewedAt = r.ReviewedAt,
        ReviewNote = r.ReviewNote, ResolvedPayrollId = r.ResolvedPayrollId, CompletedAt = r.CompletedAt };
}
