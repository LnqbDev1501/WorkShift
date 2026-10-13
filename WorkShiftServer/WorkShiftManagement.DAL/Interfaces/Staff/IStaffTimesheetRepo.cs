using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffTimesheetRepo
{
    Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default);

    Task<List<WorkScheduleWeek>> GetAvailableWeeksForStaffAsync(int branchId, CancellationToken cancellationToken = default);

    Task<WorkScheduleWeek?> GetWeeklyScheduleWithStaffShiftsAsync(int scheduleWeekId, int branchId, CancellationToken cancellationToken = default);

    Task<Timesheet?> GetStaffTimesheetForWeekAsync(int scheduleWeekId, Guid staffId, CancellationToken cancellationToken = default);

    Task<bool> ConfirmTimesheetAsync(int timesheetId, Guid staffId, string? note, CancellationToken cancellationToken = default);
}
