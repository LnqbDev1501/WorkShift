using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class EmployeeAdjustmentRepo(AppDbContext db) : IEmployeeAdjustmentRepo
{
    public Task<User?> ActorAsync(Guid id, CancellationToken ct) => db.Users.AsNoTracking().Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == id, ct);
    public Task<User?> StaffAsync(Guid id, int branchId, CancellationToken ct) => db.Users.AsNoTracking().Include(u => u.Role)
        .SingleOrDefaultAsync(u => u.UserId == id && u.BranchId == branchId, ct);
    public Task<bool> StaffHasContractAsync(Guid userId, int branchId, CancellationToken ct) =>
        db.EmploymentContracts.AsNoTracking().AnyAsync(c =>
            c.UserId == userId &&
            (c.BranchId == branchId || db.Users.Any(u => u.UserId == userId && u.BranchId == branchId)) &&
            (c.Status == "ACTIVE" || c.Status == "SCHEDULED" ||
             c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL"), ct);
    public Task<PayrollPeriod?> PeriodAsync(int id, int? branchId, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .SingleOrDefaultAsync(p => p.PayrollPeriodId == id && (!branchId.HasValue || p.BranchId == branchId), ct);
        public Task<PayrollPeriod?> FindPeriodByDateAsync(int branchId, DateOnly date, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => p.BranchId == branchId && p.StartDate <= date && date <= p.EndDate)
        .OrderByDescending(p => p.PayrollPeriodId)
        .FirstOrDefaultAsync(ct);
    public Task<List<PayrollPeriod>> PeriodsAsync(int branchId, IReadOnlyCollection<string> statuses, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => p.BranchId == branchId && statuses.Contains(p.Status))
        .OrderByDescending(p => p.StartDate).ThenByDescending(p => p.PayrollPeriodId).ToListAsync(ct);
    private static IQueryable<EmployeeRewardsPenalty> Details(IQueryable<EmployeeRewardsPenalty> q) => q
        .Include(a => a.User).ThenInclude(u => u.Role).Include(a => a.PayrollPeriod).Include(a => a.PayrollDetails).Include(a => a.CreatedByNavigation)
        .Include(a => a.ApprovedByNavigation).Include(a => a.RejectedByNavigation).AsSplitQuery();
    public Task<EmployeeRewardsPenalty?> FindAsync(int id, string type, int? branchId, bool tracking, CancellationToken ct)
    {
        var q = db.EmployeeRewardsPenalties.Where(a => a.RewardPenaltyId == id && a.AdjustmentType == type
            && !a.IsDeleted && (!branchId.HasValue || a.User.BranchId == branchId));
        return Details(tracking ? q : q.AsNoTracking()).SingleOrDefaultAsync(ct);
    }
    public async Task<(List<EmployeeRewardsPenalty> Items, int Count)> ListAsync(string type, EmployeeAdjustmentFilter f, CancellationToken ct)
    {
        var q = db.EmployeeRewardsPenalties.AsNoTracking().Where(a => !a.IsDeleted && a.AdjustmentType == type);
        if (f.BranchId.HasValue) q = q.Where(a => a.User.BranchId == f.BranchId);
        if (f.UserId.HasValue) q = q.Where(a => a.UserId == f.UserId);
        if (f.PeriodId.HasValue) q = q.Where(a => a.PayrollPeriodId == f.PeriodId);
        if (f.Status != null) q = q.Where(a => a.ApprovalStatus == f.Status);
        if (f.From.HasValue) q = q.Where(a => a.CreatedAt >= f.From);
        if (f.ToExclusive.HasValue) q = q.Where(a => a.CreatedAt < f.ToExclusive);
        if (f.Keyword != null)
        {
            var keyword = "%" + f.Keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var exactId = Guid.TryParse(f.Keyword, out var userId);
            q = q.Where(a => EF.Functions.ILike(a.User.FullName, keyword, "\\")
                || EF.Functions.ILike(a.AdjustmentName, keyword, "\\")
                || (a.PayrollPeriod != null && EF.Functions.ILike(a.PayrollPeriod.PeriodCode, keyword, "\\")) || exactId && a.UserId == userId);
        }
        var count = await q.CountAsync(ct);
        return (await Details(q.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.RewardPenaltyId)
            .Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)).ToListAsync(ct), count);
    }
    public Task<bool> HasPayrollSourceAsync(int id, CancellationToken ct) => db.PayrollDetails.AnyAsync(d => d.RewardPenaltyId == id, ct);
    public async Task<IDbContextTransaction> BeginChangeAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.ChangeTracker.Clear();
            // Same lock order as calculation/approval/settlement; serialize pending decisions and payroll reads.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.payroll_periods, public.payrolls IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.employee_rewards_penalties IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE public.users, public.roles IN SHARE MODE", ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
    public void Add(EmployeeRewardsPenalty item) => db.EmployeeRewardsPenalties.Add(item);
    public static string Snapshot(EmployeeRewardsPenalty item) => JsonSerializer.Serialize(new
    { item.UserId, item.PayrollPeriodId, item.AdjustmentDate, item.AdjustmentType, item.AdjustmentName, item.Amount, item.Reason,
        item.ApprovalStatus, item.ApprovedBy, item.ApprovedAt, item.RejectedBy, item.RejectedAt, item.ReviewNote,
        item.IsDeleted, item.DeletedBy, item.DeletedAt });
    public void Audit(Guid actor, string action, EmployeeRewardsPenalty item, string? oldValue) => db.AuditLogs.Add(new AuditLog
    { UserId = actor, Action = action, TableName = "employee_rewards_penalties", RecordId = item.RewardPenaltyId.ToString(),
        OldValue = oldValue, NewValue = Snapshot(item), CreatedAt = DateTime.UtcNow });
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
