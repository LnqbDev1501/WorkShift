using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IPublicHolidayRepo
{
    Task<string?> GetActiveRoleNameAsync(Guid userId, CancellationToken ct = default);

    Task<(IReadOnlyList<PublicHolidayConfig> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        int? year, 
        int? month, 
        string? search,
        bool includeInactive, bool? isActive, 
        CancellationToken ct = default);

    Task<PublicHolidayConfig?> GetByIdAsync(
        int id, 
        bool includeInactive, 
        CancellationToken ct = default);

    // Must be called inside ExecuteInTransactionAsync.
    Task<PublicHolidayConfig?> GetForUpdateAsync(
        int id, 
        CancellationToken ct = default);

    Task<IReadOnlyList<DateOnly>> GetExistingDatesAsync(
        IReadOnlyCollection<DateOnly> dates, int? excludeId = null,
        CancellationToken ct = default);

    Task<bool> IsUsedInPayrollAsync(int id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PublicHolidayConfig> entities, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);

    Task WriteAuditAsync(Guid actorId, string action, int recordId,
        string? oldValueJson, string? newValueJson, CancellationToken ct = default);
}


