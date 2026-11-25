using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IManagerAttendanceRepo
{
    Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<List<WorkScheduleWeek>> GetAvailableWeeksForBranchAsync(int branchId, CancellationToken cancellationToken = default);

    Task<List<WorkShift>> GetShiftsForBranchAsync(int branchId, int? scheduleWeekId, DateOnly? targetDate, CancellationToken cancellationToken = default);

    Task<List<ShiftAssignment>> GetDailyAssignmentsWithDetailsAsync(
        int branchId,
        int? scheduleWeekId,
        DateOnly? targetDate,
        string? search,
        int? shiftId,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignment?> GetAssignmentAttendanceDetailAsync(
        int assignmentId,
        int branchId,
        CancellationToken cancellationToken = default);

    Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default);
}
