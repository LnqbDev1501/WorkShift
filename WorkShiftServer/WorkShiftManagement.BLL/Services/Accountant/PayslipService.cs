using WorkShiftManagement.BLL.DTOs.Accountant.Payslip;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Version.Payroll;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayslipService(IPayslipRepo repo, IPayrollCalculationRepo calculation, TimeProvider clock) : IPayslipService
{
    private async Task<(PayslipScope Scope, bool Staff)> Scope(Guid actor, CancellationToken ct)
    {
        var user = await calculation.GetActorAsync(actor, ct);
        if (user == null || user.Status != "ACTIVE" || user.DeletedAt != null || user.Role.Status != "ACTIVE")
            throw new UnauthorizedAccessException("Tài khoản không có quyền xem phiếu lương.");
        return user.Role.RoleName switch
        {
            "ADMIN" => (new PayslipScope(null, null), false),
            "ACCOUNTANT" => (new PayslipScope(user.BranchId ?? throw new UnauthorizedAccessException("Accountant chưa có chi nhánh."), null), false),
            "STAFF" => (new PayslipScope(null, actor), true),
            _ => throw new UnauthorizedAccessException("Chỉ Admin, Accountant và Staff được xem phiếu lương.")
        };
    }

    public async Task<PayslipListResponse> ListAsync(Guid actor, PayslipListRequest q, CancellationToken ct)
    {
        var (scope, staff) = await Scope(actor, ct);
        if (q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue
            || q.Keyword?.Length > 200 || q.UserId == Guid.Empty || q.BranchId <= 0 || q.PayrollPeriodId <= 0)
            throw new ArgumentException("Tham số không hợp lệ: Page >= 1, PageSize 1–100, Keyword tối đa 200 ký tự, ID phải hợp lệ.");
        if (q.PeriodFrom.HasValue && q.PeriodTo.HasValue && q.PeriodFrom > q.PeriodTo)
            throw new ArgumentException("PeriodFrom không được sau PeriodTo.");
        if (scope.UserId.HasValue && q.UserId.HasValue && q.UserId != scope.UserId
            || scope.BranchId.HasValue && q.BranchId.HasValue && q.BranchId != scope.BranchId)
            throw new UnauthorizedAccessException("Không được tra cứu ngoài phạm vi tài khoản.");
        var status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim().ToUpperInvariant();
        if (status != null && status is not ("PUBLISHED" or "REPLACED"))
            throw new ArgumentException("Payslip status chỉ gồm PUBLISHED hoặc REPLACED.");
        var (items, count) = await repo.ListAsync(scope, new PayslipSearch
        {
            Keyword = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim(),
            UserId = q.UserId, BranchId = q.BranchId, PayrollPeriodId = q.PayrollPeriodId,
            Status = status, IncludeHistory = q.IncludeHistory, PeriodFrom = q.PeriodFrom, PeriodTo = q.PeriodTo,
            Page = q.Page, PageSize = q.PageSize
        }, ct);
        var unresolved = staff ? await repo.UnresolvedAsync(items, ct) : [];
        var now = clock.GetUtcNow().UtcDateTime;
        return new PayslipListResponse
        {
            Page = q.Page, PageSize = q.PageSize, TotalItems = count,
            Items = items.Select(s => Summary(s, staff, unresolved.Contains((s.Payroll.UserId, s.Payroll.PayrollPeriodId)), now)).ToList()
        };
    }

    private async Task<Payslip> Get(PayslipScope scope, int id, CancellationToken ct)
    {
        if (id <= 0) throw new ArgumentException("Payslip ID phải lớn hơn 0.");
        return await repo.GetAsync(scope, id, ct) ?? throw new KeyNotFoundException("Không tìm thấy phiếu lương trong phạm vi được phép.");
    }

    public async Task<PayslipDetailsResponse> GetAsync(Guid actor, int id, CancellationToken ct)
    {
        var (scope, staff) = await Scope(actor, ct);
        var slip = await Get(scope, id, ct);
        var unresolved = staff ? await repo.UnresolvedAsync([slip], ct) : [];
        return new PayslipDetailsResponse
        {
            Payslip = Summary(slip, staff, unresolved.Contains((slip.Payroll.UserId, slip.Payroll.PayrollPeriodId)), clock.GetUtcNow().UtcDateTime),
            // No JOIN to Attendance/Shift or fallback to live work data.
            WorkDetails = slip.PayslipWorkDetails.OrderBy(w => w.DisplayOrder).ThenBy(w => w.PayslipWorkDetailId)
                .Select(w => new PayslipWorkDetailsResponse
                {
                    PayslipWorkDetailId = w.PayslipWorkDetailId, SourceAssignmentId = w.SourceAssignmentId,
                    SourceAttendanceId = w.SourceAttendanceId, WorkDate = w.WorkDate, ShiftName = w.ShiftNameSnapshot,
                    ScheduledStartAt = w.ScheduledStartAt, ScheduledEndAt = w.ScheduledEndAt,
                    BreakMinutes = w.BreakMinutesSnapshot, CheckInTime = w.CheckInTime, CheckOutTime = w.CheckOutTime,
                    AttendanceStatus = w.AttendanceStatusSnapshot, ActualWorkingMinutes = w.ActualWorkingMinutes,
                    PayableMinutes = w.PayableMinutes, TotalLateMinutes = w.TotalLateMinutes,
                    ExcusedLateMinutes = w.ExcusedLateMinutes, ChargeableLateMinutes = w.ChargeableLateMinutes,
                    TotalEarlyLeaveMinutes = w.TotalEarlyLeaveMinutes, ExcusedEarlyLeaveMinutes = w.ExcusedEarlyLeaveMinutes,
                    ChargeableEarlyLeaveMinutes = w.ChargeableEarlyLeaveMinutes, Note = w.Note, DisplayOrder = w.DisplayOrder
                }).ToList(),
            CalculationDetails = PayrollCalculationService.MapDetails(slip.Payroll)
        };
    }

    public async Task<PayslipVersionHistoryResponse> VersionsAsync(Guid actor, int id, CancellationToken ct)
    {
        var (scope, staff) = await Scope(actor, ct);
        var slip = await Get(scope, id, ct);
        var versions = await repo.VersionsAsync(scope, slip.Payroll.UserId, slip.Payroll.PayrollPeriodId, ct);
        var unresolved = staff ? await repo.UnresolvedAsync(versions, ct) : [];
        var now = clock.GetUtcNow().UtcDateTime;
        return new PayslipVersionHistoryResponse
        {
            UserId = slip.Payroll.UserId, PayrollPeriodId = slip.Payroll.PayrollPeriodId,
            CurrentPayslipId = versions.FirstOrDefault(s => s.Status == "PUBLISHED")?.PayslipId,
            Versions = versions.Select(s => Summary(s, staff, unresolved.Contains((s.Payroll.UserId, s.Payroll.PayrollPeriodId)), now)).ToList()
        };
    }

    public static PayslipSummaryResponse Summary(Payslip s, bool staff, bool unresolved, DateTime now)
    {
        var p = s.Payroll;
        var blocked = PayslipPolicy.FeedbackBlocked(s, staff, unresolved, now);
        return new PayslipSummaryResponse
        {
            PayslipId = s.PayslipId, PayrollId = s.PayrollId, PayrollPeriodId = p.PayrollPeriodId, UserId = p.UserId,
            BranchId = p.BranchId, EmployeeName = p.EmployeeFullNameSnapshot, EmployeePosition = p.EmployeePositionSnapshot,
            BranchName = p.BranchNameSnapshot, PeriodCode = s.PeriodCodeSnapshot, PeriodName = s.PeriodNameSnapshot,
            PeriodStart = s.PeriodStartSnapshot, PeriodEnd = s.PeriodEndSnapshot, VersionNo = s.VersionNo,
            PreviousPayslipId = s.PreviousPayslipId, Status = s.Status, PayrollStatus = p.Status,
            PublishedAt = s.PublishedAt, ReplacedAt = s.ReplacedAt, FeedbackDeadlineAt = p.PayrollPeriod.InitialFeedbackDeadlineAt ?? s.PublishedAt,
            CanSubmitFeedback = blocked == null, FeedbackBlockedReason = blocked, CorrectionReason = p.CorrectionReason,
            HourlyRate = p.HourlyRateSnapshot, TotalWorkHours = p.TotalWorkHours, WorkPay = p.WorkPay,
            TotalReward = p.TotalReward, TotalPenalty = p.TotalPenalty, AttendanceDeduction = p.AttendanceDeduction,
            CarryForwardDeduction = p.CarryForwardDeduction, DeferredDeductionAmount = p.DeferredDeductionAmount,
            GrossSalary = p.GrossSalary, TotalDeduction = p.TotalDeduction, NetSalary = p.NetSalary
        };
    }

    public Task<List<int>> DuePeriodIdsAsync(CancellationToken ct) => repo.DuePeriodIdsAsync(ct);

    public async Task PublishInitialAsync(int periodId, CancellationToken ct)
    {
        await using var tx = await calculation.BeginCalculationAsync(ct);
        var period = await calculation.GetPeriodAsync(periodId, null, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương cần phát hành.");
        // Transaction lock serializes publication, approval and corrections across Hangfire retries/servers.
        var all = await repo.PublicationPayrollsAsync(periodId, ct);
        var current = PayrollVersionPolicy.Current(all, period.Status);
        var expected = await repo.ExpectedEmployeesAsync(period, ct);
        if (current.Count == 0 || !expected.SetEquals(current.Select(p => p.UserId)))
            throw new InvalidOperationException("Kỳ chưa đủ Payroll hiện hành cho tất cả nhân viên; không phát hành một phần.");
        // Published OR replaced historical slips are evidence of publication. A rejected
        // unpublished payroll version does not make an already published period incomplete.
        if (current.All(p => p.Payslip != null)) { await tx.CommitAsync(ct); return; }
        if (all.Any(p => p.Payslip != null))
            throw new InvalidOperationException("Kỳ đã có Payslip nhưng chưa đủ cho các Payroll hiện hành; cần kiểm tra dữ liệu phát hành, không tự phát lại cả kỳ.");
        var now = clock.GetUtcNow().UtcDateTime;
        var scheduled = period.ApprovedAt ?? throw new InvalidOperationException("Kỳ chưa có thời điểm Admin duyệt.");
        if (period.Status != "APPROVED") throw new InvalidOperationException("Chỉ phát phiếu cho kỳ APPROVED.");
        if (current.Any(p => p.Status != "APPROVED"))
            throw new InvalidOperationException("Tất cả Payroll hiện hành phải APPROVED trước khi phát phiếu.");
        // Validate/build the whole batch before adding anything; any failure rolls back every slip.
        var slips = current.Select(p =>
        {
            p.PayrollPeriod = period;
            PayrollApprovalService.ValidateTotals(p);
            return PayslipPolicy.Create(p, now);
        }).ToList();
        foreach (var slip in slips) repo.Add(slip);
        repo.RecordPublicationAudit(periodId, scheduled, now, slips.Count);
        await calculation.SaveAsync(ct);
        await tx.CommitAsync(ct);
    }
}
