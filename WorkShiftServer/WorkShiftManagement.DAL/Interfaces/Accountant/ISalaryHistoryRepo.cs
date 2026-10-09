using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public interface ISalaryHistoryRepo
{
    Task<int?> GetActiveAccountantBranchIdAsync(Guid accountantId, CancellationToken ct = default);
    Task<(IReadOnlyList<StaffSalaryHistory> Items, int TotalCount)> GetPagedAsync(
        int branchId, 
        string? keyword, 
        Guid? userId, 
        string? status, 
        string? changeType,
        DateOnly? effectiveFrom, 
        DateOnly? effectiveTo, 
        int page, int pageSize,
        CancellationToken ct = default);
    Task<StaffSalaryHistory?> GetByIdAsync(int id, int branchId, CancellationToken ct = default);
}
