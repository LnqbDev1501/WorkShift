using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin
{
    public interface IPayrollApprovalService
    {
        Task<PayrollPeriodListResponse>
            GetPayrollPeriodsAsync(
                string? status,
                int? regionId,
                int? branchId,
                string? search,
                int pageNumber,
                int pageSize);

        Task<PayrollPeriodDetailResponse>
            GetPayrollPeriodDetailAsync(
                int payrollPeriodId,
                string? search,
                int pageNumber,
                int pageSize);

        Task<PayrollRejectResponse>
            RejectPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                RejectPayrollRequest request,
                Guid? adminId);

        Task<PayrollApprovalResponse>
            ApprovePayrollPeriodAsync(
                int payrollPeriodId,
                Guid? adminId);

        Task<PayrollPeriodSubmitResponse>
            SubmitPayrollPeriodAsync(
                int payrollPeriodId,
                Guid accountantId);

        Task<PayrollSubmitResponse>
            SubmitRejectedPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                Guid accountantId);
    }
}
