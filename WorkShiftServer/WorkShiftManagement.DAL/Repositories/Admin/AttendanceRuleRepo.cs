using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class AttendanceRuleRepo : IAttendanceRuleRepo
{
    private const string StatusDraft = "DRAFT";
    private const string StatusCancelled = "CANCELLED";

    private readonly AppDbContext _context;

    public AttendanceRuleRepo(AppDbContext context) => _context = context;

    public async Task<AttendanceRule> CreateAsync(AttendanceRule rule, CancellationToken cancellationToken)
    {
        rule.Status = StatusDraft;
        _context.AttendanceRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<AttendanceRule> UpdateAsync(AttendanceRule rule, CancellationToken cancellationToken)
    {
        _context.AttendanceRules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = ruleName.Trim().ToLower();
        return await _context.AttendanceRules
            .AsNoTracking()
            .AnyAsync(
                r => r.RuleName.ToLower() == normalizedName
                     && (!excludeId.HasValue || r.AttendanceRuleId != excludeId.Value)
                     && r.Status != StatusCancelled,
                cancellationToken);
    }

    public Task<AttendanceRule?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _context.AttendanceRules
            .AsNoTracking()
            .Include(r => r.CreatedByNavigation)
            .SingleOrDefaultAsync(r => r.AttendanceRuleId == id, cancellationToken);

    public Task<AttendanceRule?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _context.AttendanceRules
            .Include(r => r.CreatedByNavigation)
            .SingleOrDefaultAsync(r => r.AttendanceRuleId == id, cancellationToken);

    public Task<List<AttendanceRule>> GetAllAsync(CancellationToken cancellationToken) =>
        _context.AttendanceRules
            .AsNoTracking()
            .Include(r => r.CreatedByNavigation)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AttendanceRule> Items, int TotalItems)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.AttendanceRules
            .AsNoTracking()
            .Include(r => r.CreatedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(r => r.RuleName.ToLower().Contains(normalizedSearch) ||
                                     (r.Description != null && r.Description.ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(r => r.Status.ToUpper() == normalizedStatus);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }
}
