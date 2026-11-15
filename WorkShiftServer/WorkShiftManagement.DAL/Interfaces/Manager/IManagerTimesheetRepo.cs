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
        DateTime? reviewDeadline = null,
        CancellationToken cancellationToken = default);

    Task<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, List<Guid> AccountantUserIds, int NotificationId, int AccountantNotificationId, string NotificationContent, string AccountantNotificationContent)> LockWeeklyTimesheetAsync(
        int scheduleWeekId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<(List<TimesheetAdjustmentRequest> Items, int TotalCount, int PendingCount, int ApprovedCount, int RejectedCount)> GetTimesheetAdjustmentsAsync(
        int branchId,
        string? search,
        string? status,
        int? scheduleWeekId,
        Guid? staffId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TimesheetAdjustmentRequest?> GetAdjustmentDetailAsync(
        int adjustmentRequestId,
        int branchId,
        CancellationToken cancellationToken = default);

    Task<TimesheetAdjustmentRequest> ApproveAdjustmentRequestAsync(
        int adjustmentRequestId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<TimesheetAdjustmentRequest> RejectAdjustmentRequestAsync(
        int adjustmentRequestId,
        int branchId,
        Guid managerId,
        string managerNote,
        CancellationToken cancellationToken = default);

    Task<List<(WorkScheduleWeek Week, List<Timesheet> LockedTimesheets, List<Guid> StaffUserIds, int NotificationId, string NotificationContent)>> AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default);

    Task<(Timesheet Timesheet, int ScheduleWeekId, Guid StaffUserId, int NotificationId, string NotificationContent)> ReopenStaffTimesheetAsync(
        int timesheetId,
        int branchId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default);
    Task<string?> GetUserNameByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> GetDefaultDirectorNameAsync(CancellationToken cancellationToken = default);
}
