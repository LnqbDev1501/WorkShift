using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public record BranchSalesOrderRecord(
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    string StaffName);

public interface IBranchSalesReportRepo
{
    Task<Branch?> GetBranchByIdAsync(int branchId, CancellationToken cancellationToken = default);

    Task<List<BranchSalesOrderRecord>> GetBranchSalesOrdersAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<int?> GetManagerBranchIdAsync(Guid managerId, CancellationToken cancellationToken = default);
}
