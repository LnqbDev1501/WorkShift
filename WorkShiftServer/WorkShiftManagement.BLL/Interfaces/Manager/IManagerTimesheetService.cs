using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerTimesheetService
{
    Task<ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>> GetWeeklyTimesheetsAsync(
        Guid managerId,
        ManagerTimesheetListQuery query,
        CancellationToken cancellationToken = default);
}
