using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IAttendanceRuleRepo
{
    Task<AttendanceRule> CreateAsync(AttendanceRule rule, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken);
    Task<AttendanceRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<AttendanceRule>> GetAllAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<AttendanceRule> Items, int TotalItems)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken);
}
