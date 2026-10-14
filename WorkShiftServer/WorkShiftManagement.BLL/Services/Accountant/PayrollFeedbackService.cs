using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollFeedbackService(IPayrollFeedbackRepo repo, IPayrollCalculationRepo calculation,
    IPayrollRevisionService revisions, TimeProvider clock) : IPayrollFeedbackService
{
    private async Task<PayrollFeedbackScope> Scope(Guid actor, string? requiredRole, CancellationToken ct)
    {
        var user = await calculation.GetActorAsync(actor, ct);
        if (user == null || user.Status != "ACTIVE" || user.DeletedAt != null || user.Role.Status != "ACTIVE"
            || user.Role.RoleName is not ("ACCOUNTANT" or "STAFF") || requiredRole != null && user.Role.RoleName != requiredRole)
            throw new UnauthorizedAccessException("Không có quyền xử lý phản hồi phiếu lương.");
        return user.Role.RoleName == "STAFF" ? new(null, actor)
            : new(user.BranchId ?? throw new UnauthorizedAccessException("Accountant chưa có chi nhánh."), null);
    }

    private static string? Normalized(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static bool ValidReason(string? reason) => reason is "CALCULATION" or "RATE" or "REWARD" or "DEDUCTION" or "OTHER";
    private static void Positive(int id) { if (id <= 0) throw new ArgumentException("ID phải lớn hơn 0."); }

    public async Task<PayrollFeedbackResponse> SubmitAsync(Guid actor, int payslipId, SubmitPayrollFeedbackRequest request, CancellationToken ct)
    {
        var scope = await Scope(actor, "STAFF", ct);
        Positive(payslipId);
        var reason = Normalized(request?.ReasonCode);
        if (request == null || !ValidReason(reason) || string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 2000)
            throw new ArgumentException("Chọn CALCULATION, RATE, REWARD, DEDUCTION hoặc OTHER; mô tả bắt buộc, tối đa 2000 ký tự.");
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var slip = await repo.GetPayslipAsync(payslipId, scope, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu lương thuộc tài khoản.");
        var unresolved = await repo.HasUnresolvedAsync(actor, slip.Payroll.PayrollPeriodId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var blocked = PayslipPolicy.FeedbackBlocked(slip, true, unresolved, now);
        if (blocked != null) throw new InvalidOperationException(blocked);
        if (!await repo.IsCurrentPayslipAsync(slip, ct)) throw new InvalidOperationException("Chỉ gửi phản hồi trên phiếu hiện hành.");
        if (slip.Payroll.UserId != actor || slip.PeriodEndSnapshot != slip.Payroll.PayrollPeriod.EndDate
            || slip.PeriodStartSnapshot != slip.Payroll.PayrollPeriod.StartDate
            || now < PayslipPolicy.PublicationAt(slip.PeriodEndSnapshot))
            throw new InvalidOperationException("Phiếu lương không thuộc lịch phát hoặc dữ liệu kỳ không nhất quán.");
        var feedback = new PayrollFeedbackRequest
        {
            EmployeeId = actor, PayrollId = slip.PayrollId, Payroll = slip.Payroll, PayslipId = payslipId, Payslip = slip,
            ReasonCode = reason!, Description = request.Description.Trim(), Status = "SUBMITTED", CreatedAt = now, UpdatedAt = now
        };
        repo.Add(feedback);
        await calculation.SaveAsync(ct); // Obtain DB-generated ID before audit, still within the same transaction.
        repo.Audit(actor, "SUBMIT_PAYROLL_FEEDBACK", feedback, now);
        await calculation.SaveAsync(ct);
        await tx.CommitAsync(ct);
        return Map(feedback);
    }

    public async Task<PayrollFeedbackListResponse> ListAsync(Guid actor, PayrollFeedbackListRequest q, CancellationToken ct)
    {
        var scope = await Scope(actor, null, ct);
        if (q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue
            || q.Keyword?.Length > 200 || q.UserId == Guid.Empty || q.PayrollPeriodId <= 0 || q.PayslipId <= 0
            || q.CreatedTo == DateOnly.MaxValue || q.CreatedFrom.HasValue && q.CreatedTo.HasValue && q.CreatedFrom > q.CreatedTo)
            throw new ArgumentException("Tham số tìm kiếm/phân trang/khoảng ngày không hợp lệ.");
        if (scope.EmployeeId.HasValue && q.UserId.HasValue && q.UserId != scope.EmployeeId)
            throw new UnauthorizedAccessException("Staff chỉ được xem phản hồi của mình.");
        var status = Normalized(q.Status); var correction = Normalized(q.CorrectionStatus); var reason = Normalized(q.ReasonCode);
        if (status != null && status is not ("SUBMITTED" or "APPROVED" or "REJECTED")
            || correction != null && correction is not ("PENDING" or "IN_PROGRESS" or "COMPLETED")
            || reason != null && !ValidReason(reason))
            throw new ArgumentException("Status, CorrectionStatus hoặc ReasonCode không hợp lệ.");
        static DateTime Utc(DateOnly date) => new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
        var (items, count) = await repo.ListAsync(scope, new PayrollFeedbackSearch
        {
            Keyword = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim(), UserId = q.UserId,
            PayrollPeriodId = q.PayrollPeriodId, PayslipId = q.PayslipId, Status = status, CorrectionStatus = correction, ReasonCode = reason,
            CreatedFromUtc = q.CreatedFrom.HasValue ? Utc(q.CreatedFrom.Value) : null,
            CreatedToExclusiveUtc = q.CreatedTo.HasValue ? Utc(q.CreatedTo.Value.AddDays(1)) : null, Page = q.Page, PageSize = q.PageSize
        }, ct);
        return new() { Page = q.Page, PageSize = q.PageSize, TotalItems = count, Items = items.Select(Map).ToList() };
    }

    private async Task<PayrollFeedbackRequest> Feedback(int id, PayrollFeedbackScope scope, bool tracking, CancellationToken ct)
    {
        Positive(id);
        return await repo.GetAsync(id, scope, tracking, ct) ?? throw new KeyNotFoundException("Không tìm thấy phản hồi trong phạm vi được phép.");
    }

    public async Task<PayrollFeedbackDetailsResponse> GetAsync(Guid actor, int id, CancellationToken ct)
    {
        var f = await Feedback(id, await Scope(actor, null, ct), false, ct);
        return new() { Feedback = Map(f), OriginalPayroll = PayrollCalculationService.Map(f.Payroll),
            CorrectionPayroll = f.ResolvedPayroll == null ? null : PayrollCalculationService.Map(f.ResolvedPayroll) };
    }

    public Task<PayrollFeedbackResponse> ApproveAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
        => Review(actor, id, request, true, ct);
    public Task<PayrollFeedbackResponse> RejectAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
        => Review(actor, id, request, false, ct);

    private async Task<PayrollFeedbackResponse> Review(Guid actor, int id, ReviewPayrollFeedbackRequest request, bool approve, CancellationToken ct)
    {
        var scope = await Scope(actor, "ACCOUNTANT", ct);
        Positive(id);
        if (request == null || request.ResolutionNote?.Length > 2000 || !approve && string.IsNullOrWhiteSpace(request.ResolutionNote))
            throw new ArgumentException("Reject bắt buộc có lý do; ghi chú xử lý tối đa 2000 ký tự.");
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var f = await Feedback(id, scope, true, ct);
        if (f.Status != "SUBMITTED" || f.CorrectionStatus != null || f.ResolvedPayrollId != null)
            throw new InvalidOperationException("Chỉ được xử lý feedback SUBMITTED chưa có correction; không ghi đè kết quả cũ.");
        if (f.Payroll.PayrollPeriod.Status != "APPROVED" || f.Payroll.Status != "APPROVED"
            || f.Payslip.Status != "PUBLISHED" || f.Payslip.PayrollId != f.PayrollId || f.EmployeeId != f.Payroll.UserId
            || f.Payslip.PeriodEndSnapshot != f.Payroll.PayrollPeriod.EndDate
            || f.Payslip.PeriodStartSnapshot != f.Payroll.PayrollPeriod.StartDate
            || f.CreatedAt < f.Payslip.PublishedAt || f.CreatedAt < PayslipPolicy.PublicationAt(f.Payslip.PeriodEndSnapshot)
            || f.CreatedAt >= PayslipPolicy.FeedbackDeadlineAt(f.Payslip.PeriodEndSnapshot)
            || !await repo.IsCurrentPayslipAsync(f.Payslip, ct))
            throw new InvalidOperationException("Feedback phải gửi đúng hạn trên phiếu hiện hành của kỳ APPROVED chưa quyết toán.");
        // Processing timely feedback may continue AFTER the deadline; no new feedback is admitted then.
        var now = clock.GetUtcNow().UtcDateTime;
        f.Status = approve ? "APPROVED" : "REJECTED";
        f.CorrectionStatus = approve ? "PENDING" : null;
        f.ProcessedBy = actor; f.ProcessedAt = now; f.UpdatedAt = now;
        f.ResolutionNote = string.IsNullOrWhiteSpace(request.ResolutionNote) ? null : request.ResolutionNote.Trim();
        repo.Audit(actor, approve ? "APPROVE_PAYROLL_FEEDBACK" : "REJECT_PAYROLL_FEEDBACK", f, now);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        return Map(f);
    }

    public async Task<PayrollCalculationResponse> CreateCorrectionAsync(Guid actor, int id, PayrollCorrectionRequest request, CancellationToken ct)
    {
        var f = await Feedback(id, await Scope(actor, "ACCOUNTANT", ct), false, ct);
        if (request == null || request.FeedbackId.HasValue && request.FeedbackId != id)
            throw new ArgumentException("FeedbackId trong body phải khớp URL hoặc được bỏ trống.");
        if (f.Status != "APPROVED" || f.CorrectionStatus is not ("PENDING" or "IN_PROGRESS"))
            throw new InvalidOperationException("Cần feedback APPROVED đang chờ hoặc xử lý correction.");
        // Delegate to the existing transaction-protected revision workflow. It rechecks feedback,
        // latest version, branch, period and frozen work after taking locks; no nested transaction.
        return await revisions.RecalculateAsync(actor, f.ResolvedPayrollId ?? f.PayrollId, new PayrollCorrectionRequest
        {
            FeedbackId = id, CorrectionReason = request.CorrectionReason, SalaryHistoryId = request.SalaryHistoryId,
            HolidayPayRuleId = request.HolidayPayRuleId, AttendanceDeductions = request.AttendanceDeductions
        }, ct);
    }

    public static PayrollFeedbackResponse Map(PayrollFeedbackRequest f)
    {
        if (f.EmployeeId != f.Payroll.UserId || f.Payslip.PayrollId != f.PayrollId
            || f.ResolvedPayroll != null && (f.ResolvedPayroll.UserId != f.EmployeeId
                || f.ResolvedPayroll.PayrollPeriodId != f.Payroll.PayrollPeriodId || f.ResolvedPayroll.BranchId != f.Payroll.BranchId))
            throw new InvalidOperationException("Liên kết feedback, nhân viên, Payroll hoặc correction không nhất quán.");
        return new()
        {
        FeedbackId = f.FeedbackId, EmployeeId = f.EmployeeId, EmployeeName = f.Payroll.EmployeeFullNameSnapshot,
        BranchId = f.Payroll.BranchId, BranchName = f.Payroll.BranchNameSnapshot, PayrollPeriodId = f.Payroll.PayrollPeriodId,
        PeriodCode = f.Payslip.PeriodCodeSnapshot, PeriodName = f.Payslip.PeriodNameSnapshot,
        PayrollId = f.PayrollId, PayrollVersionNo = f.Payroll.VersionNo, PayslipId = f.PayslipId,
        PayslipVersionNo = f.Payslip.VersionNo, PayslipStatus = f.Payslip.Status,
        FeedbackDeadlineAt = PayslipPolicy.FeedbackDeadlineAt(f.Payslip.PeriodEndSnapshot), ReasonCode = f.ReasonCode,
        Description = f.Description, Status = f.Status, CorrectionStatus = f.CorrectionStatus,
        ProcessedBy = f.ProcessedBy, ProcessedAt = f.ProcessedAt, ResolutionNote = f.ResolutionNote,
        ResolvedPayrollId = f.ResolvedPayrollId, ResolvedPayrollVersionNo = f.ResolvedPayroll?.VersionNo,
        ResolvedPayrollStatus = f.ResolvedPayroll?.Status, CorrectionPayslipId = f.ResolvedPayroll?.Payslip?.PayslipId,
        ResolvedAt = f.ResolvedAt, CreatedAt = f.CreatedAt, UpdatedAt = f.UpdatedAt
        };
    }
}
