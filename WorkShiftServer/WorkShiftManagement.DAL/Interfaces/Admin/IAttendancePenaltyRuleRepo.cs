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
    Task<AttendancePenaltyRule> UpdateAsync(AttendancePenaltyRule rule, CancellationToken cancellationToken);
    Task<AttendancePenaltyRule> UpdateDraftAsync(
        AttendancePenaltyRule existingRule,
        List<AttendancePenaltyLevel> newLevels,
        CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string ruleName, int? excludeId, CancellationToken cancellationToken);

    Task<AttendancePenaltyRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AttendancePenaltyRule?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);
}



