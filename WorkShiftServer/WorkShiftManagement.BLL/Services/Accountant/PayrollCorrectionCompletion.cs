using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using Issue = WorkShiftManagement.DAL.Entities.PayrollCorrectionRequest;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollSourceChangedException(PayrollSourceImpactResponse impact)
    : InvalidOperationException("Payroll calculation sources no longer match. Review sourceImpact to resolve all affected staff.")
{ public PayrollSourceImpactResponse SourceImpact { get; } = impact; }

public sealed partial class PayrollFeedbackService
{
    public sealed class CorrectionImpactEvidence
    {
        public List<string> SharedChangeKeys { get; set; } = [];
        public int? ParentCorrectionRequestId { get; set; }
    }
    private static Payroll? Candidate(PayrollReviewData d, Issue r)
    {
        var original = d.Payrolls.Single(p => p.PayrollId == r.PayrollId);
        var current = PayrollPreviewPolicy.Current(d).SingleOrDefault(p => p.UserId == original.UserId);
        if (current == null || current.PayrollId == original.PayrollId) return null;
        var node = current; var visited = new HashSet<int>();
        while (node.PreviousPayrollId.HasValue && visited.Add(node.PayrollId))
        {
            if (node.PreviousPayrollId == original.PayrollId) return current;
            node = d.Payrolls.SingleOrDefault(p => p.PayrollId == node.PreviousPayrollId)
                ?? throw new InvalidOperationException("Missing previous version in Payroll version chain.");
        }
        throw new InvalidOperationException("Current version does not belong to the correction request chain.");
    }
    private static void RequireCorrectResponse(PayrollReviewData d, Issue r, Payroll current)
    {
        var candidate = Candidate(d, r);
        if (r.Status != "RESPONDED" || r.ResponseAction != "CORRECT"
            || candidate == null && current.Status != "REJECTED"
            || candidate != null && current.Status != "CALCULATED")
            throw new InvalidOperationException("Requires RESPONDED/CORRECT request and rejected payroll or revision awaiting Accountant completion.");
    }
    private PayrollIssueResponse MapIssue(PayrollReviewData d, Issue r)
    {
        var result = Map(r);
        result.PayrollPeriodId = d.Period.PayrollPeriodId;
        result.PeriodCode = d.Period.PeriodCode ?? "";
        result.ScheduleWeekId = d.Period.ScheduleWeekId;
        var payroll = d.Payrolls.FirstOrDefault(p => p.PayrollId == r.PayrollId);
        result.EmployeeName = payroll?.EmployeeFullNameSnapshot ?? "";
        var isLeaf = PayrollPreviewPolicy.Leaf(d, r);
        var expired = r.Status == "EXPIRED" || (r.Status == "OPEN" && r.ResponseDeadlineAt <= Now);
        result.CanResend = isLeaf && PayrollPreviewPolicy.Mutable(d.Period) && expired;
        result.CanRespond = isLeaf && PayrollPreviewPolicy.Mutable(d.Period) && r.Status == "OPEN" && !expired;
        if (r.Status == "RESPONDED" && r.ResponseAction == "CORRECT")
        {
            result.CandidatePayrollId = Candidate(d, r)?.PayrollId;
            result.AwaitingAccountantCompletion = result.CandidatePayrollId.HasValue;
        }
        return result;
    }
    public async Task<PayrollCorrectionPreviewResponse> PreviewCorrectionAsync(Guid actor, int payrollId, int correctionRequestId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT");
        var d = await Data(u, await repo.PeriodForPayrollAsync(payrollId, ct), ct); Editable(d);
        var r = d.Corrections.SingleOrDefault(r => r.CorrectionRequestId == correctionRequestId)
            ?? throw new KeyNotFoundException("Correction Request not found in this payroll period.");
        if (!PayrollPreviewPolicy.Leaf(d, r)) throw new InvalidOperationException("Preview is only allowed for the latest correction request.");
        var original = d.Payrolls.Single(p => p.PayrollId == r.PayrollId);
        var current = PayrollPreviewPolicy.Current(d).Single(p => p.UserId == original.UserId);
        if (payrollId != original.PayrollId && payrollId != current.PayrollId) throw new InvalidOperationException("Payroll does not belong to this correction request.");
        RequireCorrectResponse(d, r, current);
        var next = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), current.UserId, actor, Now, true);
        next.PreviousPayrollId = current.PayrollId; next.VersionNo = checked(current.VersionNo + 1); next.CorrectionReason = r.Reason;
        var preview = PayrollCalculationService.Map(next); preview.Status = "PREVIEW"; preview.CalculatedAt = null; preview.CalculatedBy = null;
        return new() { CorrectionRequestId = r.CorrectionRequestId, CurrentPayroll = PayrollCalculationService.Map(current),
            Preview = preview, HasChanges = PayrollPreviewPolicy.Changed(current, next) };
    }
    public async Task<PayrollIssueResponse> CompleteCorrectionAsync(Guid actor, int id, CompletePayrollCorrectionRequest request, CancellationToken ct)
    {
        var note = Text(request.Note); var u = await Actor(actor, ct, "ACCOUNTANT");
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(id, ct), ct);
        var r = d.Corrections.Single(r => r.CorrectionRequestId == id);
        if (!PayrollPreviewPolicy.Leaf(d, r)) throw new InvalidOperationException("Only the latest correction request can be completed.");
        if (r.Status == "COMPLETED" && r.ResolvedPayrollId == request.PayrollId) { await tx.CommitAsync(ct); return MapIssue(d, r); }
        Editable(d);
        var candidate = Candidate(d, r) ?? throw new InvalidOperationException("No saved revision available for Accountant review.");
        RequireCorrectResponse(d, r, candidate);
        if (request.PayrollId != candidate.PayrollId) throw new InvalidOperationException("Must verify and complete against the current active Payroll version.");
        var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), candidate.UserId, actor, Now, true);
        if (PayrollPreviewPolicy.Changed(candidate, rebuilt)) throw new InvalidOperationException("Sources changed after revision calculation; preview and recalculation required before completion.");
        PayrollApprovalService.ValidateTotals(candidate);
        await RegisterRelated(d, r, actor, ct);
        Complete(d, r, candidate, actor, note);
        Audit(actor, "COMPLETE_AND_SEND_PAYROLL_CORRECTION", d, new { id, candidate.PayrollId, note });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return MapIssue(d, r);
    }
    private PayrollSourceImpactResponse AssessSources(PayrollReviewData d, PayrollCalculationInputs input, Guid actor, bool includePending = true)
    {
        var response = new PayrollSourceImpactResponse { PayrollPeriodId = d.Period.PayrollPeriodId };
        var current = PayrollPreviewPolicy.Current(d);
        foreach (var user in input.EmployeeIds.Union(current.Select(p => p.UserId)).Distinct())
        {
            var p = current.SingleOrDefault(p => p.UserId == user);
            var row = new PayrollSourceImpactEmployee { UserId = user, EmployeeName = p?.EmployeeFullNameSnapshot ?? user.ToString(), PayrollId = p?.PayrollId, VersionNo = p?.VersionNo,
                CorrectionRequestId = d.Corrections.LastOrDefault(r => r.Payroll.UserId == user && r.Status is "OPEN" or "RESPONDED")?.CorrectionRequestId };
            if (p == null) row.BlockingReasons.Add("No current Payroll record found.");
            else
            {
                try
                {
                    PayrollApprovalService.ValidateTotals(p);
                    var rebuilt = PayrollCalculationPolicy.Build(input, user, actor, Now, true);
                    row.HasChanges = PayrollPreviewPolicy.Changed(p, rebuilt);
                    row.ChangedSources = PayrollSourceImpactPolicy.Changes(p, rebuilt);
                    row.SharedChangeKeys = PayrollSourceImpactPolicy.SharedChanges(p, rebuilt);
                }
                catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
                { row.BlockingReasons.Add(ex.Message); }
            }
            if (includePending && input.Adjustments.Any(a => a.UserId == user && !a.IsDeleted && a.ApprovalStatus == "PENDING"))
                row.BlockingReasons.Add("Pending rewards/penalties exist that are not included in payroll or approved.");
            response.Employees.Add(row);
        }
        return response;
    }
    public async Task<PayrollSourceImpactResponse> SourceImpactAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); var d = await Data(u, periodId, ct); Editable(d);
        return AssessSources(d, await calculation.GetInputsAsync(d.Period, ct), actor);
    }
    public async Task<PayrollSourceImpactResponse> RegisterRelatedImpactAsync(Guid actor, int correctionRequestId, CancellationToken ct)
    {
        var u = await Actor(actor, ct, "ACCOUNTANT"); await using var tx = await calculation.BeginCalculationAsync(ct);
        var d = await Data(u, await repo.PeriodForCorrectionAsync(correctionRequestId, ct), ct); Editable(d);
        var r = d.Corrections.Single(r => r.CorrectionRequestId == correctionRequestId);
        if (r.Status != "RESPONDED" || r.ResponseAction != "CORRECT") throw new InvalidOperationException("Scope expansion is only allowed from requests where Manager selected CORRECT.");
        var result = await RegisterRelated(d, r, actor, ct);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return result;
    }
    private async Task<PayrollSourceImpactResponse> RegisterRelated(PayrollReviewData d, Issue parent, Guid actor, CancellationToken ct)
    {
        var input = await calculation.GetInputsAsync(d.Period, ct);
        var impact = AssessSources(d, input, actor);
        var json = await repo.CorrectionImpactAsync(parent.CorrectionRequestId, ct);
        var evidence = json == null ? null : JsonSerializer.Deserialize<CorrectionImpactEvidence>(json);
        // Legacy requests without captured evidence cannot be used to bypass a deadline.
        if (evidence == null || evidence.SharedChangeKeys.Count == 0) return impact;
        foreach (var row in impact.Employees.Where(e => e.HasChanges && e.PayrollId != parent.PayrollId))
        {
            row.RelatedToSelectedCorrection = row.SharedChangeKeys.Intersect(evidence.SharedChangeKeys, StringComparer.Ordinal).Any();
            if (!row.RelatedToSelectedCorrection || row.CorrectionRequestId.HasValue) continue;
            var p = Current(d, row.PayrollId!.Value);
            var rebuilt = PayrollCalculationPolicy.Build(input, p.UserId, actor, Now, true);
            if (!PayrollSourceImpactPolicy.OnlyCapturedSharedChanges(p, rebuilt, evidence.SharedChangeKeys))
            {
                row.BlockingReasons.Add("Additional source changes detected outside registered shared sources; automatic scope expansion denied after deadline.");
                continue;
            }
            // Do not overwrite an unprocessed Staff feedback decision.
            var feedback = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
            if (feedback?.ReviewStatus == "FEEDBACK_PENDING")
            { row.BlockingReasons.Add("Pending staff feedback exists that Accountant must resolve before expanding correction."); continue; }
            if (d.Period.InitialFeedbackDeadlineAt.HasValue && (feedback == null || PayrollPreviewPolicy.Root(d, feedback).ClosedAt.HasValue))
            {
                feedback = new PayrollFeedbackRequest { Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId,
                    RoundNo = 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now, ReviewStatus = "SUPERSEDED",
                    ReviewDeadlineAt = d.Period.InitialFeedbackDeadlineAt, ResultNote = $"Affected by shared source from Correction Request #{parent.CorrectionRequestId}." };
                repo.Add(feedback); d.Reviews.Add(feedback); await calculation.SaveAsync(ct);
            }
            else if (feedback != null) { feedback.ReviewStatus = "SUPERSEDED"; feedback.UpdatedAt = Now; }
            var related = NewIssue(d, p, actor, $"Shared source impact from request #{parent.CorrectionRequestId}: {parent.Reason}", feedback);
            // Manager's verified shared-source correction applies to this same change too.
            related.Status = "RESPONDED"; related.ResponseAction = "CORRECT"; related.ResponseNote = parent.ResponseNote;
            related.RespondedBy = parent.RespondedBy; related.RespondedAt = Now; related.UpdatedAt = Now;
            await calculation.SaveAsync(ct);
            var relatedKeys = row.SharedChangeKeys.Intersect(evidence.SharedChangeKeys, StringComparer.Ordinal).ToList();
            repo.Audit(actor, "PAYROLL_CORRECTION_SOURCE_IMPACT", "payroll_correction_requests", related.CorrectionRequestId,
                new CorrectionImpactEvidence { SharedChangeKeys = relatedKeys, ParentCorrectionRequestId = parent.CorrectionRequestId }, Now);
            Audit(actor, "REGISTER_RELATED_PAYROLL_SOURCE_IMPACT", d, new { parentCorrectionRequestId = parent.CorrectionRequestId, related.CorrectionRequestId, p.PayrollId, keys = relatedKeys });
            row.CorrectionRequestId = related.CorrectionRequestId; row.RegisteredNow = true;
        }
        return impact;
    }
}
