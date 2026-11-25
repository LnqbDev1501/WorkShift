using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public class RequestLimitRuleRepo(AppDbContext db) : IRequestLimitRuleRepo
{
    public Task<User?> GetManagerAsync(Guid id, CancellationToken ct) => db.Users.AsNoTracking()
        .Include(x => x.Role).Include(x => x.Branch)
        .FirstOrDefaultAsync(x => x.UserId == id && x.DeletedAt == null, ct);

    public async Task<(List<RequestLimitRule> Items, int Total)> GetPagedAsync(int branchId, string? type, string? status, int page, int size, CancellationToken ct)
    {
        var query = db.RequestLimitRules.AsNoTracking().Where(x => x.BranchId == branchId);
        if (type != null) query = query.Where(x => x.RequestType == type);
        if (status != null) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.RequestLimitRuleId)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    public Task<RequestLimitRule?> GetAsync(int branchId, int id, CancellationToken ct) =>
        db.RequestLimitRules.AsNoTracking().FirstOrDefaultAsync(x => x.BranchId == branchId && x.RequestLimitRuleId == id, ct);

    public async Task<RequestLimitRule> MutateAsync(int branchId, Func<List<RequestLimitRule>, RequestLimitRule> mutation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize lifecycle changes for each branch across API calls and worker instances.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT branch_id FROM public.branches WHERE branch_id = {branchId} FOR UPDATE", ct);
        var rules = await db.RequestLimitRules.Where(x => x.BranchId == branchId).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        foreach (var next in rules.Where(x => x.Status == "SCHEDULED" && x.EffectiveFrom <= today).OrderBy(x => x.EffectiveFrom))
        {
            RetireActive(rules, next);
            await db.SaveChangesAsync(ct);
            next.Status = "ACTIVE";
            next.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        var result = mutation(rules);
        if (result.RequestLimitRuleId == 0) db.RequestLimitRules.Add(result);
        // End the old version first to satisfy the unique ACTIVE index.
        var added = db.Entry(result).State == EntityState.Added;
        if (added) db.Entry(result).State = EntityState.Detached;
        var activating = result.Status == "ACTIVE";
        if (activating) result.Status = "SCHEDULED";
        await db.SaveChangesAsync(ct);
        if (activating) result.Status = "ACTIVE";
        if (added) db.RequestLimitRules.Add(result);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<int> SynchronizeAsync(DateOnly today, CancellationToken ct)
    {
        var activated = 0;
        var branches = await db.RequestLimitRules.AsNoTracking()
            .Where(x => x.Status == "SCHEDULED" && x.EffectiveFrom <= today)
            .Select(x => x.BranchId).Distinct().ToListAsync(ct);
        foreach (var branchId in branches)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT branch_id FROM public.branches WHERE branch_id = {branchId} FOR UPDATE", ct);
            var rules = await db.RequestLimitRules.Where(x => x.BranchId == branchId).ToListAsync(ct);
            // Persist inactive versions before activating successors (unique partial index).
            foreach (var next in rules.Where(x => x.Status == "SCHEDULED" && x.EffectiveFrom <= today).OrderBy(x => x.EffectiveFrom))
            {
                RetireActive(rules, next);
                await db.SaveChangesAsync(ct);
                next.Status = "ACTIVE";
                activated++;
                next.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
        }
        return activated;
    }

    private static void RetireActive(List<RequestLimitRule> rules, RequestLimitRule next)
    {
        foreach (var old in rules.Where(x => x.Status == "ACTIVE" && x.RequestType == next.RequestType))
        {
            old.Status = "INACTIVE";
            old.EffectiveTo = next.EffectiveFrom.AddDays(-1);
            old.UpdatedAt = DateTime.UtcNow;
        }
    }
}
