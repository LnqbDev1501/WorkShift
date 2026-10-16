namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IBranchSalesReportService
{
    Task<byte[]> GenerateBranchSalesReportAsync(
        int branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);
}
