using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IAttendanceService
{
    Task<List<StaffAttendanceHistoryResponse>> GetStaffAttendanceHistoryAsync(Guid userId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
    Task<EligibleShiftResponse?> GetEligibleShiftAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInSessionResponse> CreateCheckInSessionAsync(Guid userId, int? assignmentId = null, CancellationToken cancellationToken = default);
    Task<CheckInResponse> CheckInAsync(Guid userId, CheckInRequest request, CancellationToken cancellationToken = default);
    Task<CheckOutSessionResponse> CreateCheckOutSessionAsync(Guid userId, int assignmentId, CancellationToken cancellationToken = default);
    Task<CheckOutResponse> CheckOutAsync(Guid userId, CheckOutRequest request, CancellationToken cancellationToken = default);
}
