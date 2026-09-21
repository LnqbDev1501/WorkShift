using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class WorkingTimeRuleRepo : IWorkingTimeRuleRepo
{
    private readonly AppDbContext _context;

    public WorkingTimeRuleRepo(AppDbContext context) => _context = context;

    public Task<List<WorkingTimeRule>> GetAllAsync(CancellationToken cancellationToken) => _context.WorkingTimeRules
        .AsNoTracking()
        .OrderByDescending(rule => rule.WorkingTimeRuleId)
        .ToListAsync(cancellationToken);

    public async Task<WorkingTimeRule> CreateVersionAsync(WorkingTimeRule rule, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        // Serialize creators, including the first version when there is no row to lock.
        await _context.Database.ExecuteSqlRawAsync("LOCK TABLE working_time_rules IN SHARE ROW EXCLUSIVE MODE", cancellationToken);
        await _context.WorkingTimeRules.Where(x => x.Status == "ACTIVE").ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.Status, "INACTIVE")
                .SetProperty(x => x.UpdatedAt, rule.CreatedAt), cancellationToken);
        _context.WorkingTimeRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return rule;
    }
}
