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
            throw new UnauthorizedAccessException("Tài khoản không có quyền xử lý lương.");
        if (!write && user.Role.RoleName == "ADMIN") return null;
        if (user.Role.RoleName != "ACCOUNTANT" || user.BranchId == null)
            throw new UnauthorizedAccessException("Chỉ Accountant được tính lương tại chi nhánh được phân công.");
        return user.BranchId;
    }

    private async Task<PayrollPeriod> Period(int id, int? branchId, CancellationToken ct)
    {
        if (id <= 0) throw new ArgumentException("Payroll Period ID không hợp lệ.");
        return await repo.GetPeriodAsync(id, branchId, ct) ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương trong phạm vi được phép.");
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
            result.Warnings.Add("Có thưởng/phạt PENDING; chỉ khoản APPROVED được đưa vào kết quả tính.");
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

    public async Task<PayrollCalculationResponse> CalculateEmployeeAsync(Guid actor, int periodId, Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID không hợp lệ.");
        var scope = await Scope(actor, true, ct);
        await using var tx = await repo.BeginCalculationAsync(ct);
        var period = await Period(periodId, scope, ct);
        if (period.Status is not ("OPEN" or "CALCULATED"))
            throw new InvalidOperationException("Kỳ phải OPEN hoặc CALCULATED; không tính đè sau khi gửi duyệt.");
        var inputs = await repo.GetInputsAsync(period, ct);
        if (!inputs.EmployeeIds.Contains(userId)) throw new KeyNotFoundException("Nhân viên không thuộc kỳ lương.");
        var existing = inputs.Payrolls.Where(p => p.UserId == userId).OrderByDescending(p => p.VersionNo)
            .ThenByDescending(p => p.PayrollId).FirstOrDefault();
        if (existing != null)
        {
            if (existing.Status != "CALCULATED") throw new InvalidOperationException("Payroll đã tồn tại; dùng luồng correction khi bị reject.");
            // Idempotent retry: use the saved snapshot/result, never silently recalculate live inputs.
            await tx.CommitAsync(ct);
            return Map(existing);
        }
        var readiness = Readiness(inputs, userId, actor);
        if (!readiness.CanCalculate) throw new PayrollCalculationBlockedException(readiness);
        var payroll = PayrollCalculationPolicy.Build(inputs, userId, actor, DateTime.UtcNow);
        repo.Add(payroll);
        await repo.SaveAsync(ct);
        repo.Audit(actor, payroll);
        var completed = inputs.EmployeeIds.All(id => id == userId || inputs.Payrolls.Any(p => p.UserId == id && p.Status == "CALCULATED"));
        if (completed)
        {
            period.Status = "CALCULATED"; period.CalculatedBy = actor;
            period.CalculatedAt = payroll.CalculatedAt; period.UpdatedAt = DateTime.UtcNow;
        }
        await repo.SaveAsync(ct);
        await tx.CommitAsync(ct);
        return Map(payroll);
    }

    public async Task<CalculationBatchResponse> CalculatePeriodAsync(Guid actor, int periodId, CancellationToken ct)
    {
        var initial = await GetReadinessAsync(actor, periodId, ct);
        if (initial.PeriodStatus is not ("OPEN" or "CALCULATED")) throw new InvalidOperationException("Kỳ phải OPEN hoặc CALCULATED.");
        if (initial.Employees.Count == 0) throw new InvalidOperationException("Kỳ chưa có nhân viên/bảng công cần tính.");
        var result = new CalculationBatchResponse { PayrollPeriodId = periodId };
        foreach (var employee in initial.Employees)
        {
            ct.ThrowIfCancellationRequested();
            try { result.Payrolls.Add(await CalculateEmployeeAsync(actor, periodId, employee.UserId, ct)); }
            catch (PayrollCalculationBlockedException ex) { result.FailedEmployees.Add(ex.Readiness); }
            // Persistence, authorization and concurrency errors abort the request; no false success.
        }
        var scope = await Scope(actor, true, ct);
        result.PeriodStatus = (await Period(periodId, scope, ct)).Status;
        return result;
    }

    public async Task<PagedResponse<PayrollCalculationResponse>> GetPayrollsAsync(Guid actor, int periodId,
        PayrollCalculationListRequest query, CancellationToken ct)
    {
        var scope = await Scope(actor, false, ct);
        await Period(periodId, scope, ct);
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentException("Page >= 1, PageSize từ 1 đến 100 và chỉ số trang không vượt giới hạn.");
        if (query.UserId == Guid.Empty || query.Keyword?.Length > 200) throw new ArgumentException("UserId/Keyword không hợp lệ.");
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();
        if (status != null && status is not ("CALCULATED" or "SUBMITTED" or "REJECTED" or "APPROVED" or "PAID" or "REPLACED"))
            throw new ArgumentException("Trạng thái Payroll không hợp lệ.");
        var (items, count) = await repo.GetPayrollsAsync(periodId, query.Keyword, query.UserId, status, query.Page, query.PageSize, ct);
        return new PagedResponse<PayrollCalculationResponse> { Items = items.Select(Map).ToList(),
            PageNumber = query.Page, PageSize = query.PageSize, TotalItems = count };
    }

    public async Task<PayrollCalculationResponse> GetPayrollAsync(Guid actor, int payrollId, CancellationToken ct)
    {
        var scope = await Scope(actor, false, ct);
        if (payrollId <= 0) throw new ArgumentException("Payroll ID không hợp lệ.");
        var payroll = await repo.GetPayrollAsync(payrollId, scope, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy Payroll trong phạm vi được phép.");
        return Map(payroll);
    }

    public static PayrollCalculationResponse Map(Payroll payroll) => new()
    {
        PayrollId = payroll.PayrollId, UserId = payroll.UserId, PayrollPeriodId = payroll.PayrollPeriodId,
        BranchId = payroll.BranchId, TimesheetId = payroll.TimesheetId, SalaryHistoryId = payroll.SalaryHistoryId,
        EmployeeName = payroll.EmployeeFullNameSnapshot, EmployeePosition = payroll.EmployeePositionSnapshot,
        BranchName = payroll.BranchNameSnapshot, Status = payroll.Status, VersionNo = payroll.VersionNo,
        PreviousPayrollId = payroll.PreviousPayrollId, CorrectionReason = payroll.CorrectionReason,
        RejectionCategory = payroll.RejectionCategory, RejectReason = payroll.RejectReason,
        CalculatedBy = payroll.CalculatedBy, CalculatedAt = payroll.CalculatedAt,
        SubmittedBy = payroll.SubmittedBy, SubmittedAt = payroll.SubmittedAt,
        RejectedBy = payroll.RejectedBy, RejectedAt = payroll.RejectedAt,
        ApprovedBy = payroll.ApprovedBy, ApprovedAt = payroll.ApprovedAt,
        ReplacedAt = payroll.ReplacedAt, PaidAt = payroll.PaidAt,
        HourlyRate = payroll.HourlyRateSnapshot, TotalWorkHours = payroll.TotalWorkHours, WorkPay = payroll.WorkPay,
        TotalReward = payroll.TotalReward, TotalPenalty = payroll.TotalPenalty, AttendanceDeduction = payroll.AttendanceDeduction,
        CarryForwardDeduction = payroll.CarryForwardDeduction, DeferredDeductionAmount = payroll.DeferredDeductionAmount,
        GrossSalary = payroll.GrossSalary, TotalDeduction = payroll.TotalDeduction, NetSalary = payroll.NetSalary,
        WorkInputSnapshot = string.IsNullOrWhiteSpace(payroll.WorkInputSnapshot) ? null : JsonSerializer.Deserialize<JsonElement>(payroll.WorkInputSnapshot),
        Details = payroll.PayrollDetails.OrderBy(d => d.DisplayOrder).ThenBy(d => d.PayrollDetailId).Select(d => new PayrollCalculationDetailResponse
        {
            PayrollDetailId = d.PayrollDetailId, Type = d.Type, ItemCode = d.ItemCode, Description = d.Description,
            WorkFrom = d.WorkFrom, WorkTo = d.WorkTo, Quantity = d.Quantity, Rate = d.Rate, Multiplier = d.Multiplier,
            Amount = d.Amount, AppliedAmount = d.AppliedAmount, SalaryHistoryId = d.SalaryHistoryId,
            PublicHolidayId = d.PublicHolidayId, HolidayPayRuleId = d.HolidayPayRuleId,
            RewardPenaltyId = d.RewardPenaltyId, AttendancePenaltyId = d.AttendancePenaltyId,
            CarryForwardId = d.CarryForwardId, DisplayOrder = d.DisplayOrder, Note = d.Note
        }).ToList()
    };
}
