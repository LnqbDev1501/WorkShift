using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class SalaryChangeRepo(AppDbContext db) : ISalaryChangeRepo
{
    public Task<User?> ActorAsync(Guid id, CancellationToken ct) => db.Users.AsNoTracking()
        .Include(u => u.Role).Include(u => u.Branch).SingleOrDefaultAsync(u => u.UserId == id, ct);
    public Task<User?> StaffAsync(Guid id, int branchId, CancellationToken ct) => db.Users.AsNoTracking()
        .Include(u => u.Role).Include(u => u.Branch).SingleOrDefaultAsync(u => u.UserId == id && u.BranchId == branchId, ct);
    private static IQueryable<SalaryChangeRequest> Details(IQueryable<SalaryChangeRequest> q) => q
        .Include(r => r.User).ThenInclude(u => u.Role).Include(r => r.User).ThenInclude(u => u.Branch)
        .Include(r => r.Branch).Include(r => r.CurrentSalaryHistory).ThenInclude(s => s.Contract)
        .Include(r => r.StaffSalaryHistory).AsSplitQuery();
    public Task<SalaryChangeRequest?> FindAsync(int id, int? branchId, bool tracking, CancellationToken ct)
    {
        var q = db.SalaryChangeRequests.Where(r => r.SalaryChangeRequestId == id && (!branchId.HasValue || r.BranchId == branchId));
        return Details(tracking ? q : q.AsNoTracking()).SingleOrDefaultAsync(ct);
    }
    public async Task<(List<SalaryChangeRequest> Items, int Count)> ListAsync(SalaryChangeFilter f, CancellationToken ct)
    {
        var q = db.SalaryChangeRequests.AsNoTracking();
        if (f.BranchId.HasValue) q = q.Where(r => r.BranchId == f.BranchId);
        if (f.UserId.HasValue) q = q.Where(r => r.UserId == f.UserId);
        if (f.Status != null) q = q.Where(r => r.Status == f.Status);
        if (f.CreatedFrom.HasValue) q = q.Where(r => r.CreatedAt >= f.CreatedFrom);
        if (f.CreatedToExclusive.HasValue) q = q.Where(r => r.CreatedAt < f.CreatedToExclusive);
        if (f.EffectiveFrom.HasValue) q = q.Where(r => r.ProposedEffectiveFrom >= f.EffectiveFrom);
        if (f.EffectiveTo.HasValue) q = q.Where(r => r.ProposedEffectiveFrom <= f.EffectiveTo);
        if (f.Keyword != null)
        {
            var pattern = "%" + f.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var exact = Guid.TryParse(f.Keyword, out var userId);
            q = q.Where(r => EF.Functions.ILike(r.User.FullName, pattern, "\\")
                || EF.Functions.ILike(r.Reason, pattern, "\\") || exact && r.UserId == userId);
        }
        var count = await q.CountAsync(ct);
        return (await Details(q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.SalaryChangeRequestId)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)).ToListAsync(ct), count);
    }
    public Task<List<StaffSalaryHistory>> SalariesAsync(IEnumerable<Guid> users, bool tracking, CancellationToken ct)
    {
        var ids = users.Distinct().ToList();
        var q = db.StaffSalaryHistories.Where(s => ids.Contains(s.UserId));
        return (tracking ? q : q.AsNoTracking()).Include(s => s.Contract).ToListAsync(ct);
    }
    public Task<EmployeeRequest?> EmployeeRequestAsync(int id, CancellationToken ct) => db.EmployeeRequests.AsNoTracking()
        .Include(r => r.SalaryIncreaseRequestDetail).SingleOrDefaultAsync(r => r.RequestId == id, ct);
    public Task<bool> PendingAsync(Guid user, CancellationToken ct) => db.SalaryChangeRequests
        .AnyAsync(r => r.UserId == user && r.Status == "PENDING", ct);
    public Task<bool> HasFinalPayrollAsync(Guid user, DateOnly from, CancellationToken ct) => db.Payrolls
        .AnyAsync(p => p.UserId == user && p.PayrollPeriod.EndDate >= from
            && (p.Status == "SUBMITTED" || p.Status == "APPROVED" || p.Status == "PAID"
                || p.PayrollPeriod.Status == "SUBMITTED" || p.PayrollPeriod.Status == "APPROVED"
                || p.PayrollPeriod.Status == "PAID" || p.PayrollPeriod.Status == "CLOSED"), ct);
    public async Task<IDbContextTransaction> BeginChangeAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.ChangeTracker.Clear();
            // Same financial lock prefix as calculation; then serialize proposals and history writers.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.employment_contracts, public.staff_salary_history, public.salary_change_requests IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.employee_requests, public.salary_increase_request_details, public.users, public.roles, public.branches IN SHARE MODE", ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
    public Task<List<StaffSalaryHistory>> DueAsync(DateOnly today, CancellationToken ct) => db.StaffSalaryHistories
        .Include(s => s.Contract).Include(s => s.User).ThenInclude(u => u.Role).Include(s => s.User).ThenInclude(u => u.Branch)
        .Include(s => s.SalaryChangeRequest)
        .Where(s => s.Status == "SCHEDULED" && s.ChangeType == "INCREASE" && s.SalaryChangeRequestId != null && s.EffectiveFrom <= today)
        .OrderBy(s => s.EffectiveFrom).ThenBy(s => s.SalaryHistoryId).ToListAsync(ct);
    public void Add(SalaryChangeRequest item) => db.SalaryChangeRequests.Add(item);
    public void Add(StaffSalaryHistory item) => db.StaffSalaryHistories.Add(item);
    public void Audit(Guid? actor, string action, string table, string recordId, string? oldValue, string newValue, DateTime now)
        => db.AuditLogs.Add(new AuditLog { UserId = actor, Action = action, TableName = table, RecordId = recordId,
            OldValue = oldValue, NewValue = newValue, CreatedAt = now });
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
