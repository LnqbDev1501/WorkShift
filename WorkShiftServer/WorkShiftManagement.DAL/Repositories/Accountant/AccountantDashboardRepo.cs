using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class AccountantDashboardRepo(AppDbContext db) : IAccountantDashboardRepo
{
    public Task<int?> GetActiveAccountantBranchIdAsync(Guid userId, CancellationToken ct) => db.Users.AsNoTracking()
        .Where(x => x.UserId == userId && x.DeletedAt == null && x.Status == "ACTIVE" &&
            x.Role.RoleName == "ACCOUNTANT" && x.Role.Status == "ACTIVE")
        .Select(x => x.BranchId)
        .SingleOrDefaultAsync(ct);

    public IQueryable<PayrollPeriod> PayrollPeriods(int branchId) => db.PayrollPeriods.AsNoTracking()
        .Where(x => x.BranchId == branchId);
    public IQueryable<Payroll> CurrentPayrolls(int branchId) => db.Payrolls.AsNoTracking()
        .Where(x => x.BranchId == branchId && x.PayrollPeriod.BranchId == branchId &&
            x.ReplacedAt == null && x.InversePreviousPayroll == null && x.PayrollPeriod.Status != "CANCELLED");
}
