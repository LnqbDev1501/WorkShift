using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;
public sealed class PayrollRevisionRepo(AppDbContext db) : IPayrollRevisionRepo
{
    private IQueryable<Payroll> Graph => db.Payrolls.AsNoTracking().Include(p => p.PayrollDetails).Include(p => p.PayrollPeriod);
    public Task<Payroll?> GetAsync(int id, int? branchId, CancellationToken ct) => Graph.SingleOrDefaultAsync(p => p.PayrollId == id && (!branchId.HasValue || p.BranchId == branchId), ct);
    public Task<List<Payroll>> VersionsAsync(Guid userId, int periodId, CancellationToken ct) => Graph.Where(p => p.UserId == userId && p.PayrollPeriodId == periodId).OrderBy(p => p.VersionNo).ThenBy(p => p.PayrollId).ToListAsync(ct);
}
