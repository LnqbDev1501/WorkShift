using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollCalculationBlockedException(EmployeeCalculationReadiness readiness)
    : InvalidOperationException(string.Join(" ", readiness.BlockingReasons))
{
    public EmployeeCalculationReadiness Readiness { get; } = readiness;
}

public sealed class PayrollCalculationService(IPayrollCalculationRepo repo) : IPayrollCalculationService
{
    private async Task<int?> Scope(Guid actor, bool write, CancellationToken ct)
    {
        var user = await repo.GetActorAsync(actor, ct);
        if (user == null || user.DeletedAt != null || user.Status != "ACTIVE" || user.Role.Status != "ACTIVE")
            throw new UnauthorizedAccessException("User is not authorized to manage payroll.");
        if (!write && user.Role.RoleName == "ADMIN") return null;
        if (user.Role.RoleName != "ACCOUNTANT" || user.BranchId == null)
            throw new UnauthorizedAccessException("Only Accountants can calculate payroll for their assigned branch.");
        return user.BranchId;
    }

    private async Task<PayrollPeriod> Period(int id, int? branchId, CancellationToken ct)
    {
        if (id <= 0) throw new ArgumentException("Invalid Payroll Period ID.");
        return await repo.GetPeriodAsync(id, branchId, ct) ?? throw new KeyNotFoundException("Payroll period not found within authorized scope.");
    }

    private static EmployeeCalculationReadiness Readiness(PayrollCalculationInputs inputs, Guid user, Guid actor)
    {
        var existing = inputs.Payrolls.Where(p => p.UserId == user).OrderByDescending(p => p.VersionNo)
            .ThenByDescending(p => p.PayrollId).FirstOrDefault();
        var sheet = inputs.Timesheets.FirstOrDefault(t => t.UserId == user);
        var result = new EmployeeCalculationReadiness
        {
            UserId = user, EmployeeName = sheet?.User.FullName
                ?? inputs.Assignments.FirstOrDefault(a => a.UserId == user)?.User.FullName ?? user.ToString(),
            TimesheetId = sheet?.TimesheetId, ExistingPayrollId = existing?.PayrollId, ExistingPayrollStatus = existing?.Status
        };
        if (inputs.Adjustments.Any(a => a.UserId == user && !a.IsDeleted && a.ApprovalStatus == "PENDING"))
            result.Warnings.Add("Pending rewards/penalties exist; only APPROVED items are included in calculation results.");
        try { PayrollCalculationPolicy.Build(inputs, user, actor, DateTime.UtcNow); result.CanCalculate = true; }
        catch (InvalidOperationException ex) { result.BlockingReasons.Add(ex.Message); }
        return result;
    }

    public async Task<CalculationReadinessResponse> GetReadinessAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var scope = await Scope(actor, true, ct);
        var period = await Period(periodId, scope, ct);
        var inputs = await repo.GetInputsAsync(period, ct);
        return new CalculationReadinessResponse { PayrollPeriodId = periodId, PeriodStatus = period.Status,
            Employees = inputs.EmployeeIds.Select(u => Readiness(inputs, u, actor)).OrderBy(r => r.EmployeeName).ThenBy(r => r.UserId).ToList() };
    }

    public async Task<CalculationInputResponse> GetEmployeeInputsAsync(Guid actor, int periodId, Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Invalid User ID.");
        var scope = await Scope(actor, true, ct);
        var period = await Period(periodId, scope, ct);
        var input = await repo.GetInputsAsync(period, ct);
        if (!input.EmployeeIds.Contains(userId)) throw new KeyNotFoundException("Staff member does not belong to this payroll period.");
        var result = new CalculationInputResponse { PayrollPeriodId = periodId, PeriodStatus = period.Status,
            BranchName = period.BranchNameSnapshot, Readiness = Readiness(input, userId, actor) };
        // Raw, available sources for inspection. They are not a successful Payroll calculation.
        foreach (var t in input.Timesheets.Where(t => t.UserId == userId))
            result.Sources.Add(new() { Type = "TIMESHEET", SourceId = t.TimesheetId, Description = "Timesheet", Status = t.Status, Minutes = t.TotalWorkMinutes });
        foreach (var a in input.Assignments.Where(a => a.UserId == userId).OrderBy(a => a.Shift.ScheduledStartAt))
        {
            result.Sources.Add(new() { Type = "WORK", SourceId = a.AssignmentId, Description = a.Shift.ShiftNameSnapshot,
                Status = a.Attendance?.Status ?? "MISSING_ATTENDANCE", WorkDate = DateOnly.FromDateTime(a.Shift.ScheduledStartAt.AddHours(7)),
                StartAt = a.Shift.ScheduledStartAt, EndAt = a.Shift.ScheduledEndAt,
                CheckIn = a.Attendance?.CheckInTime, CheckOut = a.Attendance?.CheckOutTime, Minutes = a.Attendance?.ActualWorkingMinutes });
            foreach (var penalty in (a.Attendance?.AttendancePenaltyRecords ?? [])
                .Where(p => string.Equals(p.Status, "APPLIED", StringComparison.OrdinalIgnoreCase)))
                result.Sources.Add(new() { Type = "ATTENDANCE_DEDUCTION", SourceId = penalty.AttendancePenaltyId,
                    Description = penalty.ViolationType, Amount = penalty.PenaltyAmount, Status = penalty.Status });
        }
        foreach (var salary in input.Salaries.Where(s => s.UserId == userId).OrderBy(s => s.EffectiveFrom))
            result.Sources.Add(new() { Type = "SALARY", SourceId = salary.SalaryHistoryId, Description = salary.Reason ?? "Hourly salary rate",
                Rate = salary.HourlyRate, Status = salary.Status, EffectiveFrom = salary.EffectiveFrom, EffectiveTo = salary.EffectiveTo });
        foreach (var adjustment in input.Adjustments.Where(a => a.UserId == userId && !a.IsDeleted).OrderBy(a => a.CreatedAt))
            result.Sources.Add(new() { Type = adjustment.AdjustmentType, SourceId = adjustment.RewardPenaltyId,
                Description = adjustment.AdjustmentName + " · " + adjustment.Reason, Amount = adjustment.Amount, Status = adjustment.ApprovalStatus ?? "" });
        foreach (var carry in input.CarryForwards.Where(c => c.UserId == userId).OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId))
            result.Sources.Add(new() { Type = "CARRY_FORWARD", SourceId = carry.CarryForwardId, Description = carry.Reason ?? "Carry Forward",
                Amount = carry.OriginalAmount, RemainingAmount = carry.RemainingAmount, Status = carry.Status });
        foreach (var holiday in input.Holidays.OrderBy(h => h.HolidayDate))
            result.Sources.Add(new() { Type = "HOLIDAY", SourceId = holiday.PublicHolidayId, Description = holiday.HolidayName,
                WorkDate = holiday.HolidayDate, Status = holiday.IsActive ? "ACTIVE" : "INACTIVE" });
        foreach (var rule in input.HolidayRules.OrderBy(r => r.EffectiveFrom))
            result.Sources.Add(new() { Type = "HOLIDAY_RULE", SourceId = rule.HolidayPayRuleId, Description = rule.RuleName,
                Multiplier = rule.PayMultiplier, Status = rule.Status, EffectiveFrom = rule.EffectiveFrom, EffectiveTo = rule.EffectiveTo });
        return result;
    }

    // Use the same pure policy as persistence, without Add, Save, Audit or balance updates.
    public async Task<PayrollCalculationResponse> PreviewEmployeeAsync(Guid actor, int periodId, Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Invalid User ID.");
        var scope = await Scope(actor, true, ct);
        var period = await Period(periodId, scope, ct);
        var inputs = await repo.GetInputsAsync(period, ct);
        if (!inputs.EmployeeIds.Contains(userId)) throw new KeyNotFoundException("Staff member does not belong to this payroll period.");
        var ready = Readiness(inputs, userId, actor);
        if (!ready.CanCalculate) throw new PayrollCalculationBlockedException(ready);
        return Preview(PayrollCalculationPolicy.Build(inputs, userId, actor, DateTime.UtcNow));
    }

    public async Task<CalculationBatchResponse> PreviewPeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var scope = await Scope(actor, true, ct);
        var period = await Period(periodId, scope, ct);
        if (period.Status is not ("OPEN" or "CALCULATED")) throw new InvalidOperationException("Period must be in OPEN or CALCULATED status.");
        var inputs = await repo.GetInputsAsync(period, ct);
        var result = new CalculationBatchResponse { PayrollPeriodId = periodId, PeriodStatus = period.Status };
        foreach (var userId in inputs.EmployeeIds.Where(id => !inputs.Payrolls.Any(p => p.UserId == id)))
        {
            ct.ThrowIfCancellationRequested();
            var ready = Readiness(inputs, userId, actor);
            if (!ready.CanCalculate) { result.FailedEmployees.Add(ready); continue; }
            result.Payrolls.Add(Preview(PayrollCalculationPolicy.Build(inputs, userId, actor, DateTime.UtcNow)));
        }
        return result;
    }

    private static PayrollCalculationResponse Preview(Payroll payroll)
    {
        var result = Map(payroll);
        result.Status = "PREVIEW";
        result.CalculatedBy = null;
        result.CalculatedAt = null;
        return result;
    }

    public Task<PayrollCalculationResponse> CalculateEmployeeAsync(Guid actor, int periodId, Guid userId, CancellationToken ct)
        => throw new InvalidOperationException("Initial calculation must cover the entire period. Rejected payrolls must use /payrolls/{id}/recalculate.");

    public async Task<CalculationBatchResponse> CalculatePeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var branch = await Scope(actor, true, ct);
        await using var tx = await repo.BeginCalculationAsync(ct);
        var period = await Period(periodId, branch, ct);
        var inputs = await repo.GetInputsAsync(period, ct);
        if (period.Status != "OPEN")
        {
            // A retry returns the persisted batch, never recalculates it from changed live sources.
            if (period.Status == "CALCULATED" && inputs.EmployeeIds.Count > 0
                && inputs.EmployeeIds.ToHashSet().SetEquals(inputs.Payrolls.Select(p => p.UserId))
                && inputs.Payrolls.All(p => p.Status == "CALCULATED"))
                return new() { PayrollPeriodId = periodId, PeriodStatus = period.Status, Payrolls = inputs.Payrolls.Select(Map).ToList() };
            throw new InvalidOperationException("Initial whole-period calculation is only allowed when period is OPEN; revisions must go through Correction Request.");
        }
        if (inputs.EmployeeIds.Count == 0 || inputs.Payrolls.Count != 0)
            throw new InvalidOperationException("Period must contain employees and have no existing Payroll records prior to initial full calculation.");
        // Build all results before persisting anything. Any source or storage failure rolls back the entire batch.
        var now = DateTime.UtcNow;
        var results = inputs.EmployeeIds.Select(id => PayrollCalculationPolicy.Build(inputs, id, actor, now)).ToList();
        foreach (var payroll in results) repo.Add(payroll);
        await repo.SaveAsync(ct);
        foreach (var payroll in results) repo.Audit(actor, payroll);
        period.Status = "CALCULATED"; period.CalculatedBy = actor; period.CalculatedAt = now; period.UpdatedAt = now;
        await repo.SaveAsync(ct); await tx.CommitAsync(ct);
        return new() { PayrollPeriodId = periodId, PeriodStatus = period.Status, Payrolls = results.Select(Map).ToList() };
    }

    public async Task<PagedResponse<PayrollCalculationResponse>> GetPayrollsAsync(Guid actor, int periodId,
        PayrollCalculationListRequest query, CancellationToken ct)
    {
        var scope = await Scope(actor, false, ct);
        await Period(periodId, scope, ct);
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentException("Page must be >= 1, PageSize between 1 and 100, and page index within range.");
        if (query.UserId == Guid.Empty || query.Keyword?.Length > 200) throw new ArgumentException("Invalid UserId or Keyword parameter.");
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();
        if (status != null && status is not ("CALCULATED" or "SUBMITTED" or "REJECTED" or "APPROVED" or "PAID" or "REPLACED"))
            throw new ArgumentException("Invalid Payroll status.");
        var (items, count) = await repo.GetPayrollsAsync(periodId, query.Keyword, query.UserId, status, query.Page, query.PageSize, ct);
        return new PagedResponse<PayrollCalculationResponse> { Items = items.Select(Map).ToList(),
            PageNumber = query.Page, PageSize = query.PageSize, TotalItems = count };
    }

    public async Task<PayrollCalculationResponse> GetPayrollAsync(Guid actor, int payrollId, CancellationToken ct)
    {
        var scope = await Scope(actor, false, ct);
        if (payrollId <= 0) throw new ArgumentException("Invalid Payroll ID.");
        var payroll = await repo.GetPayrollAsync(payrollId, scope, ct)
            ?? throw new KeyNotFoundException("Payroll record not found within authorized scope.");
        return Map(payroll);
    }

    public static PayrollCalculationResponse Map(Payroll payroll) => new()
    {
        PayrollId = payroll.PayrollId, UserId = payroll.UserId, PayrollPeriodId = payroll.PayrollPeriodId,
        BranchId = payroll.BranchId, TimesheetId = payroll.TimesheetId, SalaryHistoryId = payroll.SalaryHistoryId,
        EmployeeName = payroll.EmployeeFullNameSnapshot, EmployeePosition = payroll.EmployeePositionSnapshot,
        BranchName = payroll.BranchNameSnapshot, Status = payroll.Status, VersionNo = payroll.VersionNo,
        PreviousPayrollId = payroll.PreviousPayrollId, CorrectionReason = payroll.CorrectionReason,
        CalculatedBy = payroll.CalculatedBy, CalculatedAt = payroll.CalculatedAt,
        SubmittedBy = payroll.SubmittedBy, SubmittedAt = payroll.SubmittedAt,
        ApprovedBy = payroll.ApprovedBy, ApprovedAt = payroll.ApprovedAt,
        ReplacedAt = payroll.ReplacedAt, PaidAt = payroll.PaidAt,
        HourlyRate = payroll.HourlyRateSnapshot, TotalWorkHours = payroll.TotalWorkHours, WorkPay = payroll.WorkPay,
        TotalReward = payroll.TotalReward, TotalPenalty = payroll.TotalPenalty, AttendanceDeduction = payroll.AttendanceDeduction,
        CarryForwardDeduction = payroll.CarryForwardDeduction, DeferredDeductionAmount = payroll.DeferredDeductionAmount,
        GrossSalary = payroll.GrossSalary, TotalDeduction = payroll.TotalDeduction, NetSalary = payroll.NetSalary,
        WorkInputSnapshot = string.IsNullOrWhiteSpace(payroll.WorkInputSnapshot) ? null : JsonSerializer.Deserialize<JsonElement>(payroll.WorkInputSnapshot),
        Details = MapDetails(payroll)
    };

    public static List<PayrollCalculationDetailResponse> MapDetails(Payroll payroll)
        => payroll.PayrollDetails.OrderBy(d => d.DisplayOrder).ThenBy(d => d.PayrollDetailId).Select(d => new PayrollCalculationDetailResponse
        {
            PayrollDetailId = d.PayrollDetailId, Type = d.Type, ItemCode = d.ItemCode, Description = d.Description,
            WorkFrom = d.WorkFrom, WorkTo = d.WorkTo, Quantity = d.Quantity, Rate = d.Rate, Multiplier = d.Multiplier,
            Amount = d.Amount, AppliedAmount = d.AppliedAmount, SalaryHistoryId = d.SalaryHistoryId,
            PublicHolidayId = d.PublicHolidayId, HolidayPayRuleId = d.HolidayPayRuleId,
            RewardPenaltyId = d.RewardPenaltyId, AttendancePenaltyId = d.AttendancePenaltyId,
            CarryForwardId = d.CarryForwardId, DisplayOrder = d.DisplayOrder, Note = d.Note
        }).ToList();
}
