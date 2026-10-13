using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IManagerTimesheetRepo
{
    Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<(List<WorkScheduleWeek> Items, int TotalCount)> GetWeeklyTimesheetsAsync(
        int branchId,
        string? search,
        int? scheduleWeekId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<WorkScheduleWeek>> GetAvailableWeeksForBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task<WorkScheduleWeek?> GetWeeklyTimesheetDetailAsync(int scheduleWeekId, int branchId, CancellationToken cancellationToken = default);
    Task<(WorkScheduleWeek Week, List<Timesheet> Timesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)> GenerateWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)> LockWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default);
}
