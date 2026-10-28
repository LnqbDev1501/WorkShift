using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffTimesheetRepo
{
    Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default);

    Task<List<WorkScheduleWeek>> GetAvailableWeeksForStaffAsync(int branchId, CancellationToken cancellationToken = default);

    Task<WorkScheduleWeek?> GetWeeklyScheduleWithStaffShiftsAsync(int scheduleWeekId, int branchId, CancellationToken cancellationToken = default);

    Task<Timesheet?> GetStaffTimesheetForWeekAsync(int scheduleWeekId, Guid staffId, CancellationToken cancellationToken = default);

    Task<bool> ConfirmTimesheetAsync(int timesheetId, Guid staffId, string? note, CancellationToken cancellationToken = default);

    Task<bool> SubmitAdjustmentRequestsAsync(
        int scheduleWeekId,
        Guid staffId,
        List<StaffAdjustmentSubmissionItem> items,
        CancellationToken cancellationToken = default);

    Task<(TimesheetAdjustmentRequest Request, int ScheduleWeekId)> CancelAdjustmentRequestAsync(
        int adjustmentRequestId,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default);
}

public sealed class StaffAdjustmentSubmissionItem
{
    public int AssignmentId { get; set; }
    public int? AttendanceId { get; set; }
    public bool AdjustCheckIn { get; set; }
    public DateTime? RequestedCheckInTime { get; set; }
    public bool AdjustCheckOut { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public string Reason { get; set; } = null!;
}
