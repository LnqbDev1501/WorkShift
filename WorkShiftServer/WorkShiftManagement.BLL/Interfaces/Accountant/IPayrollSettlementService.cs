using WorkShiftManagement.BLL.DTOs.Accountant.PayrollSettlement;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;
public interface IPayrollSettlementService
{
    Task<PayrollSettlementSummaryResponse> SummaryAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PayrollSettlementConfirmationResponse> ConfirmAsync(
        Guid actor, 
        int id, 
        ConfirmPayrollSettlementRequest request, 
        CancellationToken ct
        );
    Task<CarryForwardListResponse> ListCarryAsync(
        Guid actor, 
        CarryForwardListRequest query, 
        CancellationToken ct
        );
    Task<CarryForwardResponse> GetCarryAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<CarryForwardApplicationListResponse> ApplicationsAsync(
        Guid actor, 
        int id, 
        int page, 
        int pageSize, 
        CancellationToken ct
        );
}
public interface IPayrollClosureNotifier
{
    Task NotifyAsync(int periodId);
}
