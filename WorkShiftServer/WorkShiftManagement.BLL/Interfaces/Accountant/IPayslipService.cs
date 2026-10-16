using WorkShiftManagement.BLL.DTOs.Accountant.Payslip;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IPayslipService
{
    Task<PayslipListResponse> ListAsync(
        Guid actor, 
        PayslipListRequest query, 
        CancellationToken ct
        );
    Task<PayslipDetailsResponse> GetAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PayslipVersionHistoryResponse> VersionsAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<List<int>> DuePeriodIdsAsync(CancellationToken ct);
    Task PublishInitialAsync(int periodId, CancellationToken ct);
}
