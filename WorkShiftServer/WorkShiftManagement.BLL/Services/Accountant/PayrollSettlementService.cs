using WorkShiftManagement.BLL.DTOs.Accountant.PayrollSettlement;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollSettlementService(IPayrollSettlementRepo repo, IPayrollCalculationRepo calculation,
    IPayrollClosureNotifier notifier, TimeProvider clock) : IPayrollSettlementService
{
    private async Task<int> Branch(Guid actor, CancellationToken ct)
    {
        var u = await calculation.GetActorAsync(actor, ct);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE" || u.Role.RoleName != "ACCOUNTANT" || u.BranchId == null)
            throw new UnauthorizedAccessException("Chỉ Accountant đang hoạt động và có chi nhánh được xử lý quyết toán/Carry Forward.");
        return u.BranchId.Value;
    }
    private static void Positive(int id) { if (id <= 0) throw new ArgumentException("ID phải lớn hơn 0."); }
    private async Task<PayrollSettlementData> Data(int id, int branch, bool tracking, CancellationToken ct)
    {
        Positive(id);
        return await repo.LoadAsync(id, branch, tracking, ct) ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương trong chi nhánh.");
    }
    public async Task<PayrollSettlementSummaryResponse> SummaryAsync(Guid actor, int id, CancellationToken ct)
    {
        var data = await Data(id, await Branch(actor, ct), false, ct);
        return Summary(data, PayrollSettlementPolicy.Assess(data, clock.GetUtcNow().UtcDateTime));
    }
    private static PayrollSettlementSummaryResponse Summary(PayrollSettlementData data, PayrollSettlementPlan plan) => new()
    {
        PayrollPeriodId = data.Period.PayrollPeriodId, PeriodCode = data.Period.PeriodCode, BranchId = data.Period.BranchId,
        PeriodStatus = data.Period.Status, FeedbackDeadlineAt = data.Period.InitialFeedbackDeadlineAt ?? data.Period.ApprovedAt ?? data.Period.CreatedAt,
        PaidAt = data.Period.PaidAt, ClosedAt = data.Period.ClosedAt, IsSettled = data.Period.Status is "PAID" or "CLOSED",
        CanConfirmSettlement = data.Period.Status == "APPROVED" && plan.BlockingReasons.Count == 0,
        ExpectedEmployeeCount = data.ExpectedEmployees.Count, UnresolvedFeedbackCount = data.UnresolvedFeedbackCount,
        TotalGrossSalary = plan.Current.Sum(p => p.GrossSalary), TotalDeduction = plan.Current.Sum(p => p.TotalDeduction),
        TotalNetSalary = plan.Current.Sum(p => p.NetSalary), TotalCarryForwardApplied = plan.Current.Sum(p => p.CarryForwardDeduction),
        TotalNewCarryForward = plan.Current.Sum(p => p.DeferredDeductionAmount), BlockingReasons = plan.BlockingReasons,
        Employees = plan.Current.OrderBy(p => p.EmployeeFullNameSnapshot).ThenBy(p => p.UserId).Select(p => new PayrollSettlementEmployeeResponse
        { UserId = p.UserId, EmployeeName = p.EmployeeFullNameSnapshot, PayrollId = p.PayrollId, VersionNo = p.VersionNo, Status = p.Status,
            PayslipId = p.Payslip?.PayslipId, NetSalary = p.NetSalary, CarryForwardDeduction = p.CarryForwardDeduction, DeferredDeductionAmount = p.DeferredDeductionAmount }).ToList()
    };
    public async Task<PayrollSettlementConfirmationResponse> ConfirmAsync(Guid actor, int id, ConfirmPayrollSettlementRequest request, CancellationToken ct)
    {
        var branch = await Branch(actor, ct); Positive(id);
        if (request == null || !request.ConfirmedExternalSettlement || request.PaymentReference?.Length > 100 || request.PaymentNote?.Length > 2000)
            throw new ArgumentException("Phải xác nhận toàn kỳ đã quyết toán bên ngoài; reference tối đa 100 ký tự, note tối đa 2000 ký tự.");
        PayrollSettlementSummaryResponse response; bool already;
        await using (var tx = await calculation.BeginCalculationAsync(ct))
        {
            var data = await Data(id, branch, true, ct);
            var now = clock.GetUtcNow().UtcDateTime;
            var plan = PayrollSettlementPolicy.Assess(data, now);
            if (plan.BlockingReasons.Count != 0) throw new InvalidOperationException(string.Join(" | ", plan.BlockingReasons));
            already = data.Period.Status is "PAID" or "CLOSED";
            if (!already)
            {
                foreach (var p in plan.Current)
                {
                    p.Status = "PAID"; p.PaidAt = now; p.PaymentConfirmedBy = actor; p.PaymentConfirmedAt = now; p.UpdatedAt = now;
                    p.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference) ? null : request.PaymentReference.Trim();
                    p.PaymentNote = string.IsNullOrWhiteSpace(request.PaymentNote) ? null : request.PaymentNote.Trim();
                }
                foreach (var item in plan.Applications)
                {
                    repo.Add(new PayrollCarryForwardApplication { CarryForwardId = item.Carry.CarryForwardId, CarryForward = item.Carry,
                        PayrollId = item.Payroll.PayrollId, Payroll = item.Payroll, AppliedAmount = item.Amount, AppliedAt = now });
                    item.Carry.RemainingAmount -= item.Amount; item.Carry.UpdatedAt = now;
                    item.Carry.Status = item.Carry.RemainingAmount == 0 ? "SETTLED" : "PARTIALLY_APPLIED";
                }
                foreach (var item in plan.NewDeductions)
                    repo.Add(new PayrollCarryForwardDeduction { UserId = item.Payroll.UserId, SourcePayrollId = item.Payroll.PayrollId,
                        SourcePayroll = item.Payroll, SourcePayrollDetailId = item.Detail.PayrollDetailId, SourcePayrollDetail = item.Detail,
                        OriginalAmount = item.Amount, RemainingAmount = item.Amount, Reason = item.Detail.Note ?? item.Detail.Description,
                        Status = "OPEN", CreatedAt = now, UpdatedAt = now });
                data.Period.Status = "PAID"; data.Period.PaidAt = now; data.Period.UpdatedAt = now;
                repo.Audit(actor, "CONFIRM_PAYROLL_PERIOD_SETTLEMENT", data.Period, now,
                    new { totalNetSalary = plan.Current.Sum(p => p.NetSalary), payrollIds = plan.Current.Select(p => p.PayrollId),
                        applicationCount = plan.Applications.Count, newCarryCount = plan.NewDeductions.Count, status = "PAID" });
                await calculation.SaveAsync(ct);
            }
            if (data.Period.Status != "CLOSED")
            {
                data.Period.Status = "CLOSED"; data.Period.ClosedAt = now; data.Period.ClosedBy = actor; data.Period.UpdatedAt = now;
                repo.Audit(null, "AUTO_CLOSE_PAYROLL_PERIOD", data.Period, now,
                    new { triggeredBy = actor, data.Period.PayrollPeriodId, data.Period.BranchId, data.Period.PaidAt, data.Period.ClosedAt, status = "CLOSED" });
                repo.Audit(null, "PAYROLL_SYSTEM_EVENT", data.Period, now,
                    new PayrollSystemMessage(id, data.Period.BranchId, "ADMIN", null, "Kỳ lương đã đóng", $"{data.Period.PeriodCode} đã quyết toán và CLOSED; có thể tra cứu/xuất Excel."));
                await calculation.SaveAsync(ct);
            }
            response = Summary(data, plan);
            await tx.CommitAsync(ct);
        }
        // Only after commit. The notifier queues Hangfire work; durable closure audits allow recovery.
        await notifier.NotifyAsync(id);
        return new() { AlreadySettled = already, Summary = response };
    }
    private static void Pagination(int page, int size)
    { if (page < 1 || size is < 1 or > 100 || (long)(page - 1) * size > int.MaxValue) throw new ArgumentException("Page >= 1 và PageSize từ 1 đến 100."); }
    public async Task<CarryForwardListResponse> ListCarryAsync(Guid actor, CarryForwardListRequest q, CancellationToken ct)
    {
        var branch = await Branch(actor, ct); Pagination(q.Page, q.PageSize);
        if (q.Keyword?.Length > 200 || q.UserId == Guid.Empty || q.SourcePayrollId <= 0 || q.SourcePayrollPeriodId <= 0
            || q.CreatedFrom == DateOnly.MinValue || q.CreatedTo == DateOnly.MinValue || q.CreatedTo == DateOnly.MaxValue
            || q.CreatedFrom.HasValue && q.CreatedTo.HasValue && q.CreatedFrom > q.CreatedTo)
            throw new ArgumentException("Tham số tìm kiếm/khoảng ngày không hợp lệ.");
        var status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim().ToUpperInvariant();
        if (status != null && status is not ("OPEN" or "PARTIALLY_APPLIED" or "SETTLED")) throw new ArgumentException("Carry Forward status không hợp lệ.");
        static DateTime Utc(DateOnly d) => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
        var (items, count) = await repo.ListCarryAsync(branch, new CarryForwardSearch
        { Keyword = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim(), UserId = q.UserId, Status = status,
            SourcePayrollId = q.SourcePayrollId, SourcePayrollPeriodId = q.SourcePayrollPeriodId, Page = q.Page, PageSize = q.PageSize,
            CreatedFromUtc = q.CreatedFrom.HasValue ? Utc(q.CreatedFrom.Value) : null,
            CreatedToExclusiveUtc = q.CreatedTo.HasValue ? Utc(q.CreatedTo.Value.AddDays(1)) : null }, ct);
        return new() { Page = q.Page, PageSize = q.PageSize, TotalItems = count, Items = items.Select(Map).ToList() };
    }
    private async Task<PayrollCarryForwardDeduction> Carry(Guid actor, int id, CancellationToken ct)
    {
        var branch = await Branch(actor, ct); Positive(id);
        return await repo.GetCarryAsync(id, branch, ct) ?? throw new KeyNotFoundException("Không tìm thấy Carry Forward trong chi nhánh nguồn.");
    }
    public async Task<CarryForwardResponse> GetCarryAsync(Guid actor, int id, CancellationToken ct) => Map(await Carry(actor, id, ct));
    public async Task<CarryForwardApplicationListResponse> ApplicationsAsync(Guid actor, int id, int page, int pageSize, CancellationToken ct)
    {
        Pagination(page, pageSize); var c = await Carry(actor, id, ct); PayrollSettlementPolicy.ValidateCarry(c);
        var apps = c.PayrollCarryForwardApplications.OrderByDescending(a => a.AppliedAt).ThenByDescending(a => a.ApplicationId).ToList();
        return new() { CarryForwardId = id, Page = page, PageSize = pageSize, TotalItems = apps.Count,
            Items = apps.Skip((page - 1) * pageSize).Take(pageSize).Select(a => new CarryForwardApplicationResponse
            { ApplicationId = a.ApplicationId, CarryForwardId = id, UserId = c.UserId, PayrollId = a.PayrollId, PayrollVersionNo = a.Payroll.VersionNo,
                PayrollPeriodId = a.Payroll.PayrollPeriodId, PeriodCode = a.Payroll.PayrollPeriod.PeriodCode,
                PeriodStart = a.Payroll.PayrollPeriod.StartDate, PeriodEnd = a.Payroll.PayrollPeriod.EndDate, AppliedAmount = a.AppliedAmount, AppliedAt = a.AppliedAt }).ToList() };
    }
    public static CarryForwardResponse Map(PayrollCarryForwardDeduction c)
    {
        PayrollSettlementPolicy.ValidateCarry(c);
        return new() { CarryForwardId = c.CarryForwardId, UserId = c.UserId, EmployeeName = c.SourcePayroll.EmployeeFullNameSnapshot,
            SourceBranchId = c.SourcePayroll.BranchId, SourcePayrollId = c.SourcePayrollId, SourcePayrollVersionNo = c.SourcePayroll.VersionNo,
            SourcePayrollPeriodId = c.SourcePayroll.PayrollPeriodId, SourcePeriodCode = c.SourcePayroll.PayrollPeriod.PeriodCode,
            SourcePayrollDetailId = c.SourcePayrollDetailId, SourceDetailType = c.SourcePayrollDetail.Type, SourceDetailDescription = c.SourcePayrollDetail.Description,
            SourceDeductionAmount = c.SourcePayrollDetail.Amount, SourceAppliedAmount = c.SourcePayrollDetail.AppliedAmount!.Value,
            OriginalAmount = c.OriginalAmount, RemainingAmount = c.RemainingAmount, TotalAppliedAmount = c.PayrollCarryForwardApplications.Sum(a => a.AppliedAmount),
            ApplicationCount = c.PayrollCarryForwardApplications.Count, Reason = c.Reason, Status = c.Status, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt };
    }
}
