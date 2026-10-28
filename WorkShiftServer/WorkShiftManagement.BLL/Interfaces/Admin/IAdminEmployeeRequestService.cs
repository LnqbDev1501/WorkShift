using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IAdminEmployeeRequestService
{
    Task<PagedResponse<AdminEmployeeRequestListItemResponse>> GetPagedAsync(
        AdminEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminEmployeeRequestDetailResponse> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminEmployeeRequestHistoryResponse>> GetHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default);
}
