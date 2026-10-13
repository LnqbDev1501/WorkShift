using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IStaffShiftService
{
    Task<PagedResponse<StaffShiftResponse>> GetAvailableAsync(Guid staffId, StaffShiftListQuery query, CancellationToken ct = default);
    Task<PagedResponse<StaffShiftResponse>> GetMyScheduleAsync(Guid staffId, StaffShiftListQuery query, CancellationToken ct = default);
    Task<StaffShiftResponse> GetDetailAsync(Guid staffId, int shiftId, CancellationToken ct = default);
    Task<StaffShiftRegistrationResponse> RegisterAsync(Guid staffId, int shiftId, CancellationToken ct = default);
    Task<StaffShiftRegistrationResponse> CancelAsync(Guid staffId, int shiftId, CancelShiftRegistrationRequest request, CancellationToken ct = default);
}
