using WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Request;
using WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface ISalaryHistoryService
{
    Task<PagedResponse<SalaryHistoryResponse>> GetAllAsync(
        SalaryHistoryListRequest request, Guid accountantId, CancellationToken ct = default);
    Task<SalaryHistoryResponse> GetByIdAsync(int id, Guid accountantId, CancellationToken ct = default);
}
