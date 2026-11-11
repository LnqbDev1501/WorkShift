namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IBranchSalesReportService
{
    Task<byte[]> GenerateBranchSalesReportAsync(
        int branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateManagerBranchSalesReportAsync(
        Guid managerId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<int?> GetUserBranchIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
