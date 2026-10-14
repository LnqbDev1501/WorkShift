using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IPayrollFeedbackService
{
    Task<PayrollFeedbackResponse> SubmitAsync(
        Guid actor,
         int payslipId, 
         SubmitPayrollFeedbackRequest request, 
         CancellationToken ct
         );
    Task<PayrollFeedbackListResponse> ListAsync(
        Guid actor, 
        PayrollFeedbackListRequest query, 
        CancellationToken ct
        );
    Task<PayrollFeedbackDetailsResponse> GetAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PayrollFeedbackResponse> ApproveAsync(
        Guid actor, 
        int id, 
        ReviewPayrollFeedbackRequest request, 
        CancellationToken ct
        );
    Task<PayrollFeedbackResponse> RejectAsync(
        Guid actor, 
        int id, 
        ReviewPayrollFeedbackRequest request, 
        CancellationToken ct
        );
    Task<PayrollCalculationResponse> CreateCorrectionAsync(
        Guid actor, 
        int id, 
        PayrollCorrectionRequest request, 
        CancellationToken ct
        );
}
