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
                int pageSize);

        Task<PayrollPeriod?> GetPayrollPeriodDetailAsync( int payrollPeriodId);

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

        Task<List<Guid>> GetActiveUserIdsByRoleAsync(string roleName);

        Task<int> SaveChangesAsync();
    }
}
