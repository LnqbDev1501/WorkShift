using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Manager.Attendance;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerAttendanceService
{
    Task<ManagerDailyAttendanceResponse> GetDailyAttendanceAsync(
        Guid managerId,
        ManagerDailyAttendanceQuery query,
        CancellationToken cancellationToken = default);

    Task<ManagerAttendanceDetailResponse?> GetAttendanceDetailAsync(
        Guid managerId,
        int assignmentId,
        CancellationToken cancellationToken = default);
}
