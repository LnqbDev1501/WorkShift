using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IStaffEmployeeRequestService
{
    Task<PagedResponse<StaffEmployeeRequestListItemResponse>> GetPagedAsync(
        Guid staffId,
        StaffEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> GetByIdAsync(
        Guid staffId,
        int requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffEligibleShiftResponse>> GetEligibleShiftsAsync(
        Guid staffId,
        string requestType,
        CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> SubmitLeaveRequestAsync(
        Guid staffId,
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> SubmitLateArrivalRequestAsync(
        Guid staffId,
        SubmitLateArrivalRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> SubmitEarlyLeaveRequestAsync(
        Guid staffId,
        SubmitEarlyLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> SubmitSalaryIncreaseRequestAsync(
        Guid staffId, SubmitSalaryIncreaseRequestRequest request, CancellationToken cancellationToken = default);

    Task<StaffEmployeeRequestDetailResponse> CancelRequestAsync(
        Guid staffId,
        int requestId,
        CancellationToken cancellationToken = default);

    Task<int> CancelOverduePendingRequestsAsync(
        CancellationToken cancellationToken = default);
}
