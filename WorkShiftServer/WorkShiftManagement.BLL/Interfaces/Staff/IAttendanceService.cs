using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IAttendanceService
{
    Task<EligibleShiftResponse?> GetEligibleShiftAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInSessionResponse> CreateCheckInSessionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInResponse> CheckInAsync(Guid userId, CheckInRequest request, CancellationToken cancellationToken = default);
}
