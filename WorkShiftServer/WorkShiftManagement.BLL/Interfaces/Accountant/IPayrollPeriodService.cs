using WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Request;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IPayrollPeriodService
{
    Task<PagedResponse<PayrollPeriodManagementResponse>> GetAllAsync(
        Guid actor, 
        PayrollPeriodListRequest query, 
        CancellationToken ct
        );
    Task<PayrollPeriodManagementResponse> GetByIdAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PagedResponse<AvailableScheduleWeekResponse>> GetAvailableWeeksAsync(
        Guid actor, 
        PayrollPeriodListRequest query, 
        CancellationToken ct
        );
    Task<PayrollPeriodManagementResponse> CreateAsync(
        Guid actor, 
        SavePayrollPeriodRequest request, 
        CancellationToken ct
        );
    Task<PayrollPeriodManagementResponse> UpdateAsync(
        Guid actor, 
        int id, 
        UpdatePayrollPeriodRequest request, 
        CancellationToken ct
        );
    Task DeleteAsync(
        Guid actor, 
        int id, 
        CancellationToken ct
        );
    Task<PayrollPeriodManagementResponse> OpenAsync(Guid actor, int id, CancellationToken ct);
    Task<PayrollPeriodReportResponse> ExportAsync(Guid actor, int id, CancellationToken ct);
}
