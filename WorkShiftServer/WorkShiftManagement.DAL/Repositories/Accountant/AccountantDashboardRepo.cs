using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class AccountantDashboardRepo(AppDbContext db) : IAccountantDashboardRepo
{
    public Task<bool> IsActiveAccountantAsync(Guid userId, CancellationToken ct) => db.Users.AsNoTracking()
        .AnyAsync(x => x.UserId == userId && x.DeletedAt == null && x.Status == "ACTIVE" &&
            x.Role.RoleName == "ACCOUNTANT" && x.Role.Status == "ACTIVE", ct);

    public IQueryable<PayrollPeriod> PayrollPeriods => db.PayrollPeriods.AsNoTracking();
    public IQueryable<Payroll> CurrentPayrolls => db.Payrolls.AsNoTracking()
        .Where(x => x.ReplacedAt == null && x.InversePreviousPayroll == null && x.PayrollPeriod.Status != "CANCELLED");
}
