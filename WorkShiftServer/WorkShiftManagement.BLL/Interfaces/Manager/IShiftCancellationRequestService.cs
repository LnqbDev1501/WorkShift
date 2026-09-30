using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IShiftCancellationRequestService
{
    Task<PagedResponse<ShiftCancellationRequestResponse>> GetListAsync(Guid actor, ShiftCancellationRequestListQuery query, CancellationToken ct = default);
    Task<ShiftCancellationRequestResponse> GetByIdAsync(Guid actor, int id, CancellationToken ct = default);
    Task<ShiftCancellationRequestResponse> ApproveAsync(Guid actor, int id, ReviewShiftCancellationRequest request, CancellationToken ct = default);
    Task<ShiftCancellationRequestResponse> RejectAsync(Guid actor, int id, ReviewShiftCancellationRequest request, CancellationToken ct = default);
}
