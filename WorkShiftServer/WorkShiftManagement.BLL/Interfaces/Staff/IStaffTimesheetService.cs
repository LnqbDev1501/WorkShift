using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Timesheet;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IStaffTimesheetService
{
    Task<StaffWeeklyTimesheetResponse?> GetMyWeeklyTimesheetAsync(
        Guid staffId,
        int? scheduleWeekId,
        CancellationToken cancellationToken = default);

    Task<StaffWeeklyTimesheetResponse> ConfirmTimesheetAsync(
        Guid staffId,
        int scheduleWeekId,
        string? note,
        CancellationToken cancellationToken = default);
}
