using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
namespace WorkShiftManagement.BLL.Interfaces.Accountant;
public interface IPayrollFeedbackService
{
    Task<List<PayrollFeedbackResponse>> SendPreviewsAsync(Guid actor, int periodId, CancellationToken ct);
    Task<PayrollFeedbackResponse> ConfirmAsync(Guid actor, int id, CancellationToken ct);
    Task<PayrollFeedbackResponse> SubmitAsync(Guid actor, int id, SubmitPayrollFeedbackRequest request, CancellationToken ct);
    Task<PagedResponse<PayrollFeedbackResponse>> ListAsync(Guid actor, PayrollFeedbackListRequest query, CancellationToken ct);
    Task<PayrollFeedbackDetailsResponse> GetAsync(Guid actor, int id, CancellationToken ct);
    Task<PayrollFeedbackResponse> ApproveAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct);
    Task<PayrollFeedbackResponse> RejectAsync(Guid actor, int id, ReviewPayrollFeedbackRequest request, CancellationToken ct);
    Task<PayrollIssueResponse> CreateIssueAsync(Guid actor, int payrollId, CreatePayrollIssueRequest request, CancellationToken ct);
    Task<List<PayrollIssueResponse>> IssuesAsync(Guid actor, int periodId, CancellationToken ct);
    Task<PayrollIssueResponse> RespondAsync(Guid actor, int id, RespondPayrollIssueRequest request, CancellationToken ct);
    Task<PayrollIssueResponse> ReviewIssueAsync(Guid actor, int id, ReviewPayrollIssueRequest request, CancellationToken ct);
    Task<PayrollCalculationResponse> RecalculateAsync(Guid actor, int payrollId, PayrollCorrectionRequest request, CancellationToken ct);
    Task SubmitPeriodAsync(Guid actor, int periodId, CancellationToken ct);
    Task ApprovePeriodAsync(Guid actor, int periodId, CancellationToken ct);
    Task ExpireDueAsync(CancellationToken ct);
}
