using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin
{
    public interface IPayrollApprovalRepo
    {
        Task<(List<PayrollPeriod> Items, int TotalCount)>
            GetPayrollPeriodsAsync(
                string? status,
                int? regionId,
                int? branchId,
                string? search,
                int pageNumber,
                int pageSize,
                DateOnly? startDateFrom = null,
                DateOnly? startDateTo = null);

        Task<PayrollPeriod?> GetPayrollPeriodDetailAsync(int payrollPeriodId);

        Task<(List<Payroll> Items, int TotalCount)>
            GetPayrollsByPeriodAsync(
                int payrollPeriodId,
                string? search,
                int pageNumber,
                int pageSize);

        Task<PayrollPeriod?> GetPayrollPeriodForUpdateAsync(int payrollPeriodId);

        Task<Payroll?> GetPayrollForUpdateAsync(int payrollId);

        Task<List<Payroll>> GetAllPayrollsForUpdateAsync(int payrollPeriodId);

        Task<bool> UserHasActiveRoleAsync(Guid userId, string roleName);
        Task<Payroll?> GetLatestPayrollAsync(Guid userId, int periodId);
        Task<bool> HasApprovedCorrectionFeedbackAsync(Payroll payroll);
        void Audit(Guid actor, string action, PayrollPeriod period, Payroll? payroll = null);
        Task<int?> GetAccountantBranchIdAsync(Guid userId);
        Task<int> GetExpectedEmployeeCountAsync(int periodId);
        Task<bool> HasCompleteLockedTimesheetsAsync(int periodId, IReadOnlyCollection<Guid> payrollUsers);

        Task<List<Guid>> GetActiveUserIdsByRoleAsync(string roleName);

        Task<int> SaveChangesAsync();
    }
}
