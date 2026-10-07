using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public interface IAccountantDashboardRepo
{
    Task<bool> IsActiveAccountantAsync(Guid userId, CancellationToken ct);
    IQueryable<PayrollPeriod> PayrollPeriods { get; }
    IQueryable<Payroll> CurrentPayrolls { get; }
}
