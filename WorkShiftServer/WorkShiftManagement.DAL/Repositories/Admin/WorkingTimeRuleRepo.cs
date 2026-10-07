using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Version.Rules;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class WorkingTimeRuleRepo : IWorkingTimeRuleRepo
{
    private readonly AppDbContext _context;
    public WorkingTimeRuleRepo(AppDbContext context) => _context = context;

    public Task<List<WorkingTimeRule>> GetAllAsync(CancellationToken cancellationToken) =>
        _context.WorkingTimeRules.AsNoTracking().OrderByDescending(rule => rule.WorkingTimeRuleId).ToListAsync(cancellationToken);

    public Task<WorkingTimeRule?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.WorkingTimeRules.AsNoTracking().SingleOrDefaultAsync(rule => rule.WorkingTimeRuleId == id, cancellationToken);

    public async Task<WorkingTimeRule> CreateVersionAsync(WorkingTimeRule rule, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        await ValidateScheduleAsync(0, rule.EffectiveFrom, cancellationToken);
        rule.Status = WorkingTimeRuleLifecycle.Draft;
        _context.WorkingTimeRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return rule;
    }

    public Task<WorkingTimeRule> UpdateDraftAsync(int id, WorkingTimeRule values, CancellationToken cancellationToken) =>
        MutateAsync(id, async rule =>
        {
            WorkingTimeRuleLifecycle.RequireStatus(rule, WorkingTimeRuleLifecycle.Draft);
            await ValidateScheduleAsync(id, values.EffectiveFrom, cancellationToken);
            rule.RuleName = values.RuleName;
            rule.MaxDailyMinutes = values.MaxDailyMinutes;
            rule.MaxWeeklyMinutes = values.MaxWeeklyMinutes;
            rule.EffectiveFrom = values.EffectiveFrom;
        }, cancellationToken);

    public async Task DeleteDraftAsync(int id, CancellationToken cancellationToken) =>
        await MutateAsync(id, rule =>
        {
            WorkingTimeRuleLifecycle.RequireStatus(rule, WorkingTimeRuleLifecycle.Draft);
            _context.WorkingTimeRules.Remove(rule);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<WorkingTimeRule> PublishAsync(int id, CancellationToken cancellationToken) =>
        MutateAsync(id, async rule =>
        {
            WorkingTimeRuleLifecycle.RequireStatus(rule, WorkingTimeRuleLifecycle.Draft);
            // MutateAsync holds the shared publication lock until commit, so concurrent
            // publishers cannot both pass this check.
            var publishedVersion = await _context.WorkingTimeRules.AsNoTracking()
                .FirstOrDefaultAsync(other => other.Status == WorkingTimeRuleLifecycle.Published, cancellationToken);
            WorkingTimeRuleLifecycle.EnsurePublicationAvailable(publishedVersion);
            await ValidateScheduleAsync(id, rule.EffectiveFrom, cancellationToken);
            rule.Status = WorkingTimeRuleLifecycle.Published;
        }, cancellationToken);

    public Task<WorkingTimeRule> CancelAsync(int id, CancellationToken cancellationToken) =>
        MutateAsync(id, rule =>
        {
            WorkingTimeRuleLifecycle.RequireStatus(rule, WorkingTimeRuleLifecycle.Published);
            rule.Status = WorkingTimeRuleLifecycle.Cancelled;
            return Task.CompletedTask;
        }, cancellationToken);

    // Serialize all state changes across API instances, including the first activation.
    private async Task<WorkingTimeRule> MutateAsync(int id, Func<WorkingTimeRule, Task> mutation, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        await ActivateDueLockedAsync(cancellationToken);
        var rule = await _context.WorkingTimeRules.SingleOrDefaultAsync(x => x.WorkingTimeRuleId == id, cancellationToken)
            ?? throw new KeyNotFoundException("Working time version was not found.");
        await mutation(rule);
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await ActivateDueLockedAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return rule;
    }

    public async Task ActivateDueAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        await ActivateDueLockedAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task LockAsync(CancellationToken cancellationToken) =>
        _context.Database.ExecuteSqlRawAsync("LOCK TABLE public.working_time_rules IN SHARE ROW EXCLUSIVE MODE", cancellationToken);

    private async Task ValidateScheduleAsync(int id, DateOnly? date, CancellationToken cancellationToken)
    {
        WorkingTimeRuleLifecycle.ValidateEffectiveDate(date, WorkingTimeRuleLifecycle.Today);
        var existing = await _context.WorkingTimeRules.AsNoTracking()
            .Where(rule => rule.WorkingTimeRuleId != id && rule.Status != WorkingTimeRuleLifecycle.Cancelled)
            .ToListAsync(cancellationToken);
        WorkingTimeRuleLifecycle.ValidateSchedule(id, date!.Value, existing);
    }

    private async Task ActivateDueLockedAsync(CancellationToken cancellationToken)
    {
        var today = WorkingTimeRuleLifecycle.Today;
        var due = await _context.WorkingTimeRules
            .Where(rule => rule.Status == WorkingTimeRuleLifecycle.Published && rule.EffectiveFrom <= today)
            .OrderBy(rule => rule.EffectiveFrom).ThenBy(rule => rule.WorkingTimeRuleId).ToListAsync(cancellationToken);
        foreach (var next in due)
        {
            var active = await _context.WorkingTimeRules.Where(rule => rule.Status == WorkingTimeRuleLifecycle.Active)
                .ToListAsync(cancellationToken);
            foreach (var previous in active)
            {
                WorkingTimeRuleLifecycle.Retire(previous, next, DateTime.UtcNow);
            }
            // Flush deactivation first to respect the unique Active index.
            await _context.SaveChangesAsync(cancellationToken);
            WorkingTimeRuleLifecycle.Activate(next, today, DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
