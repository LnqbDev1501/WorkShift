using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Accountant.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class AccountantDashboardService(IAccountantDashboardRepo repo) : IAccountantDashboardService
{
    public async Task<AccountantDashboardResponse> GetAsync(
        Guid accountantId, 
        CancellationToken ct = default
        )
    {
        if (accountantId == Guid.Empty 
        || !await repo.IsActiveAccountantAsync(accountantId, ct))
            throw new UnauthorizedAccessException("An active Accountant account and role are required.");

        // Queries share one scoped DbContext and must execute sequentially.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearEnd = yearStart.AddYears(1);
        var monthlySalaries = await repo.CurrentPayrolls
            .Where(x => x.PayrollPeriod.EndDate >= yearStart && x.PayrollPeriod.EndDate < yearEnd)
            .GroupBy(x => x.PayrollPeriod.EndDate.Month)
            .Select(g => new AccountantDashboardMonthlySalaryResponse
            {
                Month = g.Key, PayrollCount = g.Count(), GrossSalary = g.Sum(x => x.GrossSalary),
                TotalDeduction = g.Sum(x => x.TotalDeduction), NetSalary = g.Sum(x => x.NetSalary)
            }).OrderBy(x => x.Month).ToListAsync(ct);
        var periods = await repo.PayrollPeriods.GroupBy(x => x.Status)
            .Select(g => new AccountantDashboardPeriodStatusResponse { Status = g.Key, Count = g.Count() })
            .OrderBy(x => x.Status).ToListAsync(ct);
        var payrolls = await repo.CurrentPayrolls.GroupBy(x => x.Status)
            .Select(g => new AccountantDashboardPayrollStatusResponse
            {
                Status = g.Key, Count = g.Count(), GrossSalary = g.Sum(x => x.GrossSalary),
                TotalDeduction = g.Sum(x => x.TotalDeduction), NetSalary = g.Sum(x => x.NetSalary)
            }).OrderBy(x => x.Status).ToListAsync(ct);
        var recent = await repo.PayrollPeriods.Where(x => x.Status != "CANCELLED")
            .OrderByDescending(x => x.EndDate).ThenByDescending(x => x.PayrollPeriodId).Take(5)
            .Select(x => new AccountantDashboardRecentPeriodResponse
            {
                PayrollPeriodId = x.PayrollPeriodId, BranchId = x.BranchId, BranchName = x.BranchNameSnapshot,
                PeriodCode = x.PeriodCode, PeriodName = x.PeriodName, StartDate = x.StartDate, EndDate = x.EndDate,
                Status = x.Status,
                TotalPayrolls = x.Payrolls.Count(p => p.ReplacedAt == null && p.InversePreviousPayroll == null),
                TotalNetSalary = x.Payrolls.Where(p => p.ReplacedAt == null && p.InversePreviousPayroll == null)
                    .Sum(p => (decimal?)p.NetSalary) ?? 0m
            }).ToListAsync(ct);

        return new AccountantDashboardResponse
        {
            Date = today, MonthlySalaries = monthlySalaries,
            TotalPayrollPeriods = periods.Sum(x => x.Count), PayrollPeriodsByStatus = periods,
            TotalPayrolls = payrolls.Sum(x => x.Count), PayrollsByStatus = payrolls,
            CalculatedPayrolls = payrolls.FirstOrDefault(x => x.Status == "CALCULATED")?.Count ?? 0,
            SubmittedPayrolls = payrolls.FirstOrDefault(x => x.Status == "SUBMITTED")?.Count ?? 0,
            RejectedPayrolls = payrolls.FirstOrDefault(x => x.Status == "REJECTED")?.Count ?? 0,
            AwaitingPaymentPayrolls = payrolls.FirstOrDefault(x => x.Status == "APPROVED")?.Count ?? 0,
            AwaitingPaymentNetSalary = payrolls.FirstOrDefault(x => x.Status == "APPROVED")?.NetSalary ?? 0m,
            RecentPayrollPeriods = recent
        };
    }
}
