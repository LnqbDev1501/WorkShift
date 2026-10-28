using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerEmployeeRequestService
{
    Task<PagedResponse<ManagerEmployeeRequestListItemResponse>> GetPagedAsync(
        Guid managerId,
        ManagerEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<ManagerEmployeeRequestDetailResponse> GetByIdAsync(
        Guid managerId,
        int requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagerEmployeeRequestHistoryResponse>> GetHistoryAsync(
        Guid managerId,
        int requestId,
        CancellationToken cancellationToken = default);

    Task<ManagerEmployeeRequestDetailResponse> ApproveAsync(
        Guid managerId,
        int requestId,
        ApproveEmployeeRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagerEmployeeRequestDetailResponse> RejectAsync(
        Guid managerId,
        int requestId,
        RejectEmployeeRequestRequest request,
        CancellationToken cancellationToken = default);
}
