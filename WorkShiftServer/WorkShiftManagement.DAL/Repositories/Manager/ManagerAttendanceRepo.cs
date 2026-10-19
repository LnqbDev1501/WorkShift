using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public class ManagerAttendanceRepo : IManagerAttendanceRepo
{
    private readonly AppDbContext _context;

    public ManagerAttendanceRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.UserId == managerId && u.Status == "ACTIVE", cancellationToken);
    }

    public Task<List<WorkScheduleWeek>> GetAvailableWeeksForBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Where(w => w.BranchId == branchId && w.Status != "CANCELLED")
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<WorkShift>> GetShiftsForBranchAsync(int branchId, int? scheduleWeekId, DateOnly? targetDate, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkShifts
            .AsNoTracking()
            .Where(s => s.ScheduleWeek.BranchId == branchId && s.Status != "CANCELLED");

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            query = query.Where(s => s.ScheduleWeekId == scheduleWeekId.Value);
        }

        if (targetDate.HasValue)
        {
            var date = targetDate.Value;
            var startOfDayUtc = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            var endOfDayUtc = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue).AddHours(-7), DateTimeKind.Utc);
            var rawStartOfDay = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var rawEndOfDay = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            query = query.Where(s => (s.ScheduledStartAt >= startOfDayUtc && s.ScheduledStartAt <= endOfDayUtc) ||
                                     (s.ScheduledStartAt >= rawStartOfDay && s.ScheduledStartAt <= rawEndOfDay));
        }

        return query
            .OrderBy(s => s.ScheduledStartAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ShiftAssignment>> GetDailyAssignmentsWithDetailsAsync(
        int branchId,
        int? scheduleWeekId,
        DateOnly? targetDate,
        string? search,
        int? shiftId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.User)
                .ThenInclude(u => u.Role)
            .Include(sa => sa.Shift)
                .ThenInclude(s => s.ScheduleWeek)
            .Include(sa => sa.Attendance)
            .Include(sa => sa.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Request)
            .Where(sa => sa.Shift.ScheduleWeek.BranchId == branchId && sa.Status != "CANCELLED");

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            query = query.Where(sa => sa.Shift.ScheduleWeekId == scheduleWeekId.Value);
        }

        if (targetDate.HasValue)
        {
            var date = targetDate.Value;
            var startOfDayUtc = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
            var endOfDayUtc = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue).AddHours(-7), DateTimeKind.Utc);
            var rawStartOfDay = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var rawEndOfDay = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            query = query.Where(sa => (sa.Shift.ScheduledStartAt >= startOfDayUtc && sa.Shift.ScheduledStartAt <= endOfDayUtc) ||
                                       (sa.Shift.ScheduledStartAt >= rawStartOfDay && sa.Shift.ScheduledStartAt <= rawEndOfDay));
        }

        if (shiftId.HasValue && shiftId.Value > 0)
        {
            query = query.Where(sa => sa.ShiftId == shiftId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(sa =>
                EF.Functions.ILike(sa.User.FullName, searchPattern) ||
                EF.Functions.ILike(sa.User.Email, searchPattern) ||
                EF.Functions.ILike(sa.Shift.ShiftNameSnapshot, searchPattern));
        }

        return query
            .OrderByDescending(sa => sa.Shift.ScheduledStartAt)
            .ThenBy(sa => sa.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<ShiftAssignment?> GetAssignmentAttendanceDetailAsync(
        int assignmentId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return _context.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.User)
                .ThenInclude(u => u.Role)
            .Include(sa => sa.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.CheckInFaceSession)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.CheckOutFaceSession)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.AttendancePenaltyRecords)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.LateArrivalRequest)
                    .ThenInclude(r => r!.Request)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.EarlyLeaveRequest)
                    .ThenInclude(r => r!.Request)
            .Include(sa => sa.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Request)
            .FirstOrDefaultAsync(sa => sa.AssignmentId == assignmentId && sa.Shift.ScheduleWeek.BranchId == branchId, cancellationToken);
    }
}
