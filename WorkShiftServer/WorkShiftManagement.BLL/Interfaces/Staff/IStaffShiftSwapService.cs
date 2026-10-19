using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IStaffShiftSwapService
{
    Task<List<StaffShiftSwapCandidateResponse>> GetCandidatesAsync(Guid actor, int sourceAssignmentId, CancellationToken ct = default);
    Task<PagedResponse<StaffShiftSwapResponse>> GetPagedAsync(Guid actor, ShiftSwapListQuery query, CancellationToken ct = default);
    Task<StaffShiftSwapResponse> GetDetailAsync(Guid actor, int requestId, CancellationToken ct = default);
    Task<StaffShiftSwapResponse> SubmitAsync(Guid actor, SubmitShiftSwapRequest request, CancellationToken ct = default);
    Task<StaffShiftSwapResponse> CancelAsync(Guid actor, int requestId, RespondShiftSwapRequest request, CancellationToken ct = default);
    Task<StaffShiftSwapResponse> AcceptAsync(Guid actor, int requestId, CancellationToken ct = default);
    Task<StaffShiftSwapResponse> RejectAsync(Guid actor, int requestId, RespondShiftSwapRequest request, CancellationToken ct = default);
}
