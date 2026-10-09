using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollRevisionService(IPayrollCalculationRepo calculation, IPayrollRevisionRepo revisions) : IPayrollRevisionService
{
    private async Task<int?> Actor(Guid actor, string? role, CancellationToken ct)
    {
        var user = await calculation.GetActorAsync(actor, ct);
        if (user == null || user.Status != "ACTIVE" || user.DeletedAt != null || user.Role.Status != "ACTIVE"
            || role != null && user.Role.RoleName != role || user.Role.RoleName is not ("ACCOUNTANT" or "ADMIN"))
            throw new UnauthorizedAccessException("Không có quyền xử lý Payroll.");
        if (user.Role.RoleName == "ADMIN") return null;
        return user.BranchId ?? throw new UnauthorizedAccessException("Accountant chưa được phân công chi nhánh.");
    }
    private async Task<Payroll> Payroll(int id, int? branch, CancellationToken ct)
    {
        if (id <= 0) throw new ArgumentException("Payroll ID không hợp lệ.");
        return await revisions.GetAsync(id, branch, ct) ?? throw new KeyNotFoundException("Không tìm thấy Payroll trong phạm vi được phép.");
    }
    public async Task<PayrollVersionHistoryResponse> VersionsAsync(Guid actor, int id, CancellationToken ct)
    {
        var p = await Payroll(id, await Actor(actor, null, ct), ct);
        var versions = await revisions.VersionsAsync(p.UserId, p.PayrollPeriodId, ct);
        return new PayrollVersionHistoryResponse
        {
            UserId = p.UserId, PayrollPeriodId = p.PayrollPeriodId,
            LatestCandidatePayrollId = versions.Last().PayrollId,
            EffectivePayrollId = versions.LastOrDefault(v => v.Status is "APPROVED" or "PAID")?.PayrollId,
            Versions = versions.Select(PayrollCalculationService.Map).ToList()
        };
    }
    public async Task<PayrollCalculationResponse> RecalculateAsync(Guid actor, int id, PayrollCorrectionRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CorrectionReason) || request.CorrectionReason.Trim().Length > 1000)
            throw new ArgumentException("Cần lý do sửa Payroll, tối đa 1000 ký tự.");
        var branch = await Actor(actor, "ACCOUNTANT", ct);
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var previous = await Payroll(id, branch, ct);
        var period = previous.PayrollPeriod;
        if (period.Status is not ("REVISION_REQUIRED" or "APPROVED"))
            throw new InvalidOperationException("Chỉ correction ở kỳ REVISION_REQUIRED hoặc APPROVED, chưa quyết toán.");
        var versions = await revisions.VersionsAsync(previous.UserId, previous.PayrollPeriodId, ct);
        if (versions.Last().PayrollId != id) throw new InvalidOperationException("Chỉ được tạo correction từ version mới nhất; không tạo nhánh song song.");
        PayrollFeedbackRequest? feedback = null;
        if (period.Status == "REVISION_REQUIRED")
        {
            if (previous.Status != "REJECTED" || request.FeedbackId != null)
                throw new InvalidOperationException("Correction trước duyệt phải xuất phát từ Payroll REJECTED.");
        }
        else
        {
            feedback = request.FeedbackId.HasValue ? await revisions.FeedbackAsync(request.FeedbackId.Value, ct) : null;
            var published = await revisions.PublishedAsync(previous.UserId, previous.PayrollPeriodId, ct);
            if (feedback == null || feedback.Status != "APPROVED" || feedback.CorrectionStatus == "COMPLETED"
                || feedback.EmployeeId != previous.UserId || published == null || feedback.PayslipId != published.PayslipId
                || feedback.PayrollId != published.PayrollId || feedback.CreatedAt >= FeedbackDeadline(period)
                || feedback.CreatedAt < published.PublishedAt
                || previous.Status is not ("APPROVED" or "REJECTED")
                || feedback.ResolvedPayrollId.HasValue && feedback.ResolvedPayrollId != previous.PayrollId)
                throw new InvalidOperationException("Cần feedback hợp lệ đã được chấp nhận trên phiếu hiện hành, gửi trước 14:00 thứ Năm; không có correction khác đang xử lý.");
        }
        var live = await calculation.GetInputsAsync(period, ct);
        var revised = PayrollRevisionPolicy.Calculate(previous, live, request, actor, DateTime.UtcNow);
        if (!PayrollApprovalService.HasCalculationChange(previous, revised))
            throw new InvalidOperationException("Không có thay đổi dữ liệu tính lương. Chỉ đổi lý do/ghi chú không tạo version mới.");
        PayrollApprovalService.ValidateCorrection(previous, revised);
        calculation.Add(revised);
        await calculation.SaveAsync(ct);
        if (feedback != null)
        {
            feedback.CorrectionStatus = "IN_PROGRESS"; feedback.ResolvedPayrollId = revised.PayrollId; feedback.UpdatedAt = DateTime.UtcNow;
        }
        revisions.Audit(actor, "CREATE_PAYROLL_CORRECTION", revised);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        return PayrollCalculationService.Map(revised);
    }
    public static DateTime FeedbackDeadline(PayrollPeriod period) => new DateTimeOffset(
        period.EndDate.AddDays(4).ToDateTime(new TimeOnly(14, 0)), TimeSpan.FromHours(7)).UtcDateTime;

    public async Task<PayrollCalculationResponse> ApproveCorrectionAsync(Guid actor, int id, CancellationToken ct)
    {
        await Actor(actor, "ADMIN", ct);
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var revised = await Payroll(id, null, ct);
        if (revised.PayrollPeriod.Status != "APPROVED" || revised.Status != "SUBMITTED" || revised.PreviousPayroll == null)
            throw new InvalidOperationException("Duyệt riêng chỉ áp dụng correction SUBMITTED của kỳ APPROVED chưa quyết toán.");
        var versions = await revisions.VersionsAsync(revised.UserId, revised.PayrollPeriodId, ct);
        if (versions.Last().PayrollId != id) throw new InvalidOperationException("Chỉ duyệt correction version mới nhất.");
        PayrollApprovalService.ValidateCorrection(revised.PreviousPayroll, revised);
        var feedback = await revisions.CorrectionFeedbackAsync(id, ct);
        var oldSlip = await revisions.PublishedAsync(revised.UserId, revised.PayrollPeriodId, ct);
        if (feedback == null || oldSlip == null || feedback.PayslipId != oldSlip.PayslipId
            || feedback.EmployeeId != revised.UserId || feedback.PayrollId != oldSlip.PayrollId
            || feedback.CreatedAt < oldSlip.PublishedAt
            || feedback.CreatedAt >= FeedbackDeadline(revised.PayrollPeriod))
            throw new InvalidOperationException("Correction không gắn với feedback đã chấp nhận và phiếu hiện hành hợp lệ.");
        var work = PayrollRevisionPolicy.Work(revised);
        var now = DateTime.UtcNow;
        revised.Status = "APPROVED"; revised.ApprovedBy = actor; revised.ApprovedAt = now; revised.UpdatedAt = now;
        var slip = new Payslip
        {
            PayrollId = id, VersionNo = oldSlip.VersionNo + 1, PreviousPayslipId = oldSlip.PayslipId,
            Status = "PUBLISHED", PublishedAt = now, CreatedAt = now, PeriodCodeSnapshot = oldSlip.PeriodCodeSnapshot,
            PeriodNameSnapshot = oldSlip.PeriodNameSnapshot, PeriodStartSnapshot = oldSlip.PeriodStartSnapshot,
            PeriodEndSnapshot = oldSlip.PeriodEndSnapshot,
            PayslipWorkDetails = work.Select((w, index) => new PayslipWorkDetail
            {
                SourceAssignmentId = w.SourceAssignmentId, SourceAttendanceId = w.SourceAttendanceId,
                WorkDate = w.WorkDate, ShiftNameSnapshot = w.ShiftNameSnapshot, ScheduledStartAt = w.ScheduledStartAt,
                ScheduledEndAt = w.ScheduledEndAt, BreakMinutesSnapshot = w.BreakMinutesSnapshot,
                CheckInTime = w.CheckInTime, CheckOutTime = w.CheckOutTime, AttendanceStatusSnapshot = w.AttendanceStatusSnapshot,
                ActualWorkingMinutes = w.ActualWorkingMinutes, PayableMinutes = w.PayableMinutes,
                TotalLateMinutes = w.TotalLateMinutes, ExcusedLateMinutes = w.ExcusedLateMinutes,
                ChargeableLateMinutes = w.ChargeableLateMinutes, TotalEarlyLeaveMinutes = w.TotalEarlyLeaveMinutes,
                ExcusedEarlyLeaveMinutes = w.ExcusedEarlyLeaveMinutes, ChargeableEarlyLeaveMinutes = w.ChargeableEarlyLeaveMinutes,
                DisplayOrder = index + 1, CreatedAt = now
            }).ToList()
        };
        oldSlip.Status = "REPLACED"; oldSlip.ReplacedAt = now;
        oldSlip.Payroll.Status = "REPLACED"; oldSlip.Payroll.ReplacedAt = now; oldSlip.Payroll.UpdatedAt = now;
        revisions.Add(slip);
        feedback.CorrectionStatus = "COMPLETED"; feedback.ResolvedAt = now; feedback.UpdatedAt = now;
        revisions.Audit(actor, "APPROVE_AND_PUBLISH_PAYROLL_CORRECTION", revised);
        await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
        return PayrollCalculationService.Map(revised);
    }
}
