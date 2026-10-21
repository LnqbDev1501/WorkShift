using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public interface IAccountantDashboardRepo
{
    Task<int?> GetActiveAccountantBranchIdAsync(Guid userId, CancellationToken ct);
    IQueryable<PayrollPeriod> PayrollPeriods(int branchId);
    IQueryable<Payroll> CurrentPayrolls(int branchId);
}
