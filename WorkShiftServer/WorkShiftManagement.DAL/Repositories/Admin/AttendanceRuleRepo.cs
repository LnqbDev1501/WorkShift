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

    public Task<List<AttendanceRule>> GetAllAsync(CancellationToken cancellationToken) =>
        _context.AttendanceRules
            .AsNoTracking()
            .Include(r => r.CreatedByNavigation)
            .OrderBy(r => r.AttendanceRuleId)
            .ToListAsync(cancellationToken);
}
