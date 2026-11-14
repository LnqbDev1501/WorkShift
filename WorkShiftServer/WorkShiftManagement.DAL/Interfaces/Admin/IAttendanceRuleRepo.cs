using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IAttendanceRuleRepo
{
    Task<AttendanceRule> CreateAsync(AttendanceRule rule, CancellationToken cancellationToken);
    Task<AttendanceRule> UpdateAsync(AttendanceRule rule, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken);
    Task<AttendanceRule?> GetRuleByEffectiveDateAsync(DateOnly effectiveFrom, int? excludeId, CancellationToken cancellationToken);
    Task<AttendanceRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AttendanceRule?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<AttendanceRule?> GetScheduledRuleAsync(CancellationToken cancellationToken);
    Task<AttendanceRule?> GetActiveRuleAsync(CancellationToken cancellationToken);
    Task<List<AttendanceRule>> GetDueScheduledRulesAsync(DateOnly today, CancellationToken cancellationToken);
    Task<List<AttendanceRule>> GetActiveRulesAsync(CancellationToken cancellationToken);
    Task<List<AttendanceRule>> GetAllAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<AttendanceRule> Items, int TotalItems)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
