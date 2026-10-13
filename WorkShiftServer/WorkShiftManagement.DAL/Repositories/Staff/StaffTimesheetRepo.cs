using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public sealed class StaffTimesheetRepo : IStaffTimesheetRepo
{
    private readonly AppDbContext _context;

    public StaffTimesheetRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetActiveStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.UserId == staffId && u.Status == "ACTIVE", cancellationToken);
    }

    public Task<List<WorkScheduleWeek>> GetAvailableWeeksForStaffAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Where(w => w.BranchId == branchId &&
                        w.Status != "CANCELLED" &&
                        (w.PayrollPeriod != null || w.Status == "REGISTRATION_CLOSED" || w.Status == "COMPLETED"))
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkScheduleWeek?> GetWeeklyScheduleWithStaffShiftsAsync(
        int scheduleWeekId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
                    .ThenInclude(a => a.Attendance)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId && w.BranchId == branchId, cancellationToken);
    }

    public async Task<Timesheet?> GetStaffTimesheetForWeekAsync(
        int scheduleWeekId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var week = await _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.PayrollPeriod)
            .FirstOrDefaultAsync(w => w.ScheduleWeekId == scheduleWeekId, cancellationToken);

        if (week?.PayrollPeriod == null)
            return null;

        return await _context.Timesheets
            .FirstOrDefaultAsync(t => t.PayrollPeriodId == week.PayrollPeriod.PayrollPeriodId && t.UserId == staffId, cancellationToken);
    }

    public async Task<bool> ConfirmTimesheetAsync(
        int timesheetId,
        Guid staffId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId && t.UserId == staffId, cancellationToken);

        if (timesheet == null)
            return false;

        if (string.Equals(timesheet.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot confirm: Timesheet is already locked.");

        if (string.Equals(timesheet.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This timesheet has already been confirmed.");

        var now = DateTime.UtcNow;
        timesheet.StaffReviewStatus = "CONFIRMED";
        timesheet.StaffReviewedAt = now;
        timesheet.StaffReviewNote = note;
        timesheet.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
