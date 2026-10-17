using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using Issue = WorkShiftManagement.DAL.Entities.PayrollCorrectionRequest;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollSourceChangedException(PayrollSourceImpactResponse impact)
    : InvalidOperationException("Nguồn tính lương không còn khớp. Xem sourceImpact để xử lý đầy đủ các Staff bị ảnh hưởng.")
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
                ?? throw new InvalidOperationException("Thiếu version trước trong chuỗi Payroll.");
        }
        throw new InvalidOperationException("Version hiện hành không thuộc chuỗi của yêu cầu sửa.");
    }
    private static void RequireCorrectResponse(PayrollReviewData d, Issue r, Payroll current)
    {
        var candidate = Candidate(d, r);
        if (r.Status != "RESPONDED" || r.ResponseAction != "CORRECT"
            || candidate == null && current.Status != "REJECTED"
            || candidate != null && current.Status != "CALCULATED")
            throw new InvalidOperationException("Cần yêu cầu RESPONDED/CORRECT và Payroll bị reject hoặc bản sửa chờ Accountant hoàn tất.");
    }
    private static PayrollIssueResponse MapIssue(PayrollReviewData d, Issue r)
    {
        var result = Map(r);
        result.PayrollPeriodId = d.Period.PayrollPeriodId;
        result.PeriodCode = d.Period.PeriodCode ?? "";
        result.ScheduleWeekId = d.Period.ScheduleWeekId;
        var payroll = d.Payrolls.FirstOrDefault(p => p.PayrollId == r.PayrollId);
        result.EmployeeName = payroll?.EmployeeFullNameSnapshot ?? "";
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
            ?? throw new KeyNotFoundException("Không tìm thấy Correction Request trong kỳ.");
        var original = d.Payrolls.Single(p => p.PayrollId == r.PayrollId);
        var current = PayrollPreviewPolicy.Current(d).Single(p => p.UserId == original.UserId);
        if (payrollId != original.PayrollId && payrollId != current.PayrollId) throw new InvalidOperationException("Payroll không thuộc yêu cầu sửa.");
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
        if (r.Status == "COMPLETED" && r.ResolvedPayrollId == request.PayrollId) { await tx.CommitAsync(ct); return MapIssue(d, r); }
        Editable(d);
        var candidate = Candidate(d, r) ?? throw new InvalidOperationException("Chưa có bản sửa đã lưu để Accountant kiểm tra.");
        RequireCorrectResponse(d, r, candidate);
        if (request.PayrollId != candidate.PayrollId) throw new InvalidOperationException("Phải kiểm tra và hoàn tất đúng Payroll version hiện hành.");
        var rebuilt = PayrollCalculationPolicy.Build(await calculation.GetInputsAsync(d.Period, ct), candidate.UserId, actor, Now, true);
        if (PayrollPreviewPolicy.Changed(candidate, rebuilt)) throw new InvalidOperationException("Nguồn đổi sau khi tính bản sửa; cần Preview và tính lại trước khi hoàn tất.");
        PayrollApprovalService.ValidateTotals(candidate);
        await RegisterRelated(d, r, actor, ct);
        Complete(d, r, candidate, actor, note);
        Audit(actor, "COMPLETE_AND_SEND_PAYROLL_CORRECTION", d, new { id, candidate.PayrollId, note });
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return MapIssue(d, r);
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
            if (p == null) row.BlockingReasons.Add("Chưa có Payroll hiện hành.");
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
                row.BlockingReasons.Add("Còn thưởng/phạt PENDING, chưa được đưa vào lương và chưa được Submit/Approve.");
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
        if (r.Status != "RESPONDED" || r.ResponseAction != "CORRECT") throw new InvalidOperationException("Chỉ mở rộng phạm vi từ yêu cầu Manager đã CORRECT.");
        var result = await RegisterRelated(d, r, actor, ct);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct); return result;
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
                row.BlockingReasons.Add("Có thay đổi khác ngoài phạm vi nguồn chung đã ghi nhận; không tự mở rộng case sau hạn.");
                continue;
            }
            // Do not overwrite an unprocessed Staff feedback decision.
            var feedback = d.Reviews.Where(f => f.EmployeeId == p.UserId && PayrollPreviewPolicy.Leaf(d, f)).LastOrDefault();
            if (feedback?.ReviewStatus == "FEEDBACK_PENDING")
            { row.BlockingReasons.Add("Có Staff feedback cần Accountant xử lý trước khi mở rộng correction."); continue; }
            if (d.Period.InitialFeedbackDeadlineAt.HasValue && (feedback == null || PayrollPreviewPolicy.Root(d, feedback).ClosedAt.HasValue))
            {
                feedback = new PayrollFeedbackRequest { Payroll = p, PayrollId = p.PayrollId, EmployeeId = p.UserId,
                    RoundNo = 1, SentBy = actor, CreatedAt = Now, UpdatedAt = Now, ReviewStatus = "SUPERSEDED",
                    ReviewDeadlineAt = d.Period.InitialFeedbackDeadlineAt, ResultNote = $"Bị ảnh hưởng cùng nguồn của Correction Request #{parent.CorrectionRequestId}." };
                repo.Add(feedback); d.Reviews.Add(feedback); await calculation.SaveAsync(ct);
            }
            else if (feedback != null) { feedback.ReviewStatus = "SUPERSEDED"; feedback.UpdatedAt = Now; }
            var related = NewIssue(d, p, actor, $"Ảnh hưởng cùng nguồn của yêu cầu #{parent.CorrectionRequestId}: {parent.Reason}", feedback);
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
