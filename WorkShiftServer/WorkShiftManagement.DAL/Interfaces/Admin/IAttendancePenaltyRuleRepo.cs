using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IAttendancePenaltyRuleRepo
{
    Task<(IReadOnlyList<AttendancePenaltyRule> Items, int TotalItems)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken);

    Task<AttendancePenaltyRule> CreateAsync(AttendancePenaltyRule rule, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken);
    Task<AttendancePenaltyRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
}


