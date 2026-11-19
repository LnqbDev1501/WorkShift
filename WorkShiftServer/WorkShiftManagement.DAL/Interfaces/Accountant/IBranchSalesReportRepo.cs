using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public record BranchSalesOrderRecord(
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    string StaffName);

public record BranchSalesAnalyticsData(
    int BranchId,
    string BranchName,
    int TotalOrders,
    decimal TotalRevenue,
    int CompletedOrders,
    decimal AverageOrderValue,
    double RevenueGrowthPercent,
    double OrdersGrowthPercent,
    List<BranchSalesTimelinePoint> Timeline,
    List<BranchSalesCategoryPoint> Categories);

public record BranchSalesTimelinePoint(
    string DateLabel,
    decimal Revenue,
    int Orders);

public record BranchSalesCategoryPoint(
    string CategoryName,
    decimal Revenue,
    int Quantity);

public interface IBranchSalesReportRepo
{
    Task<Branch?> GetBranchByIdAsync(int branchId, CancellationToken cancellationToken = default);

    Task<List<BranchSalesOrderRecord>> GetBranchSalesOrdersAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<int?> GetManagerBranchIdAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<Branch?> GetUserBranchAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountBranchOrdersAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<BranchSalesAnalyticsData> GetBranchSalesAnalyticsAsync(
        int branchId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken cancellationToken = default);
}
