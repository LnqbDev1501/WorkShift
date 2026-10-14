using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
namespace WorkShiftManagement.BLL.Interfaces.Accountant;
public interface IPayrollRevisionService
{
    Task<PayrollCalculationResponse> RecalculateAsync(
        Guid actor, 
        int id, 
        PayrollCorrectionRequest request, 
        CancellationToken ct
        );
    Task<PayrollVersionHistoryResponse> VersionsAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PayrollCalculationResponse> ApproveCorrectionAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
}
public sealed class PayrollVersionHistoryResponse
{
    public Guid UserId { get; set; }
    public int PayrollPeriodId { get; set; }
    public int LatestCandidatePayrollId { get; set; }
    public int? EffectivePayrollId { get; set; }
    public List<PayrollCalculationResponse> Versions { get; set; } = [];
}
