using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class AttendancePenaltyRuleRepo : IAttendancePenaltyRuleRepo
{
    private readonly AppDbContext _context;

    private const string StatusDraft = "DRAFT";
    private const string StatusCancelled = "CANCELLED";

    public AttendancePenaltyRuleRepo(AppDbContext context) => _context = context;

    public async Task<AttendancePenaltyRule> CreateAsync(AttendancePenaltyRule rule, CancellationToken cancellationToken)
    {
        rule.Status = StatusDraft;
        _context.AttendancePenaltyRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = ruleName.Trim().ToLower();
        return await _context.AttendancePenaltyRules
            .AsNoTracking()
            .AnyAsync(
                r => r.RuleName.ToLower() == normalizedName
                     && (!excludeId.HasValue || r.PenaltyRuleId != excludeId.Value)
                     && r.Status != StatusCancelled,
                cancellationToken);
    }


    public async Task<(IReadOnlyList<AttendancePenaltyRule> Items, int TotalItems)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.AttendancePenaltyRules
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(r => EF.Functions.ILike(r.RuleName, $"%{trimmedSearch}%")
                                  || (r.Description != null && EF.Functions.ILike(r.Description, $"%{trimmedSearch}%")));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(r => r.Status == normalizedStatus);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.PenaltyRuleId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<AttendancePenaltyRule?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.AttendancePenaltyRules
            .AsNoTracking()
            .Include(r => r.CreatedByNavigation)
            .Include(r => r.PublishedByNavigation)
            .Include(r => r.CancelledByNavigation)
            .Include(r => r.AttendancePenaltyLevels)
            .SingleOrDefaultAsync(r => r.PenaltyRuleId == id, cancellationToken);
}

