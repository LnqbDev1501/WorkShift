using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IPayrollCalculationService
{
    Task<CalculationInputResponse> GetEmployeeInputsAsync(
        Guid actor, 
        int periodId, 
        Guid userId, 
        CancellationToken ct);
    Task<PayrollCalculationResponse> PreviewEmployeeAsync(
        Guid actor, 
        int periodId, 
        Guid userId, 
        CancellationToken ct);
    Task<CalculationBatchResponse> PreviewPeriodAsync(
        Guid actor, 
        int periodId, 
        CancellationToken ct);
    Task<CalculationReadinessResponse> GetReadinessAsync(
        Guid actor, 
        int periodId, 
        CancellationToken ct);
    Task<PayrollCalculationResponse> CalculateEmployeeAsync(
        Guid actor, 
        int periodId, 
        Guid userId, 
        CancellationToken ct);
    Task<CalculationBatchResponse> CalculatePeriodAsync(
        Guid actor, 
        int periodId, 
        CancellationToken ct);
    Task<PagedResponse<PayrollCalculationResponse>> GetPayrollsAsync(
        Guid actor, 
        int periodId,
        PayrollCalculationListRequest query,
        CancellationToken ct);
    Task<PayrollCalculationResponse> GetPayrollAsync(
        Guid actor, 
        int payrollId, 
        CancellationToken ct
        );
}
