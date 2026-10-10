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

public sealed class ManagerTimesheetRepo : IManagerTimesheetRepo
{
    private readonly AppDbContext _context;

    public ManagerTimesheetRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetActiveManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.UserId == managerId &&
                u.Status == "ACTIVE" &&
                (u.Role.RoleName == "MANAGER" || u.Role.RoleName == "ADMIN"),
                cancellationToken);
    }

    public async Task<(List<WorkScheduleWeek> Items, int TotalCount)> GetWeeklyTimesheetsAsync(
        int branchId,
        string? search,
        int? scheduleWeekId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkScheduleWeeks
            .AsNoTracking()
            .Include(w => w.Branch)
            .Include(w => w.PayrollPeriod)
                .ThenInclude(p => p!.Timesheets)
            .Include(w => w.WorkShifts)
                .ThenInclude(s => s.ShiftAssignments)
            .Where(w => w.BranchId == branchId);

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
        {
            query = query.Where(w => w.ScheduleWeekId == scheduleWeekId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(w =>
                EF.Functions.ILike(w.Branch.BranchName, searchPattern) ||
                (w.PayrollPeriod != null && EF.Functions.ILike(w.PayrollPeriod.PeriodName, searchPattern)) ||
                (w.PayrollPeriod != null && EF.Functions.ILike(w.PayrollPeriod.PeriodCode, searchPattern)));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.WeekEndDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.WeekStartDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var upperStatus = status.Trim().ToUpperInvariant();
            query = query.Where(w =>
                w.Status == upperStatus ||
                (w.PayrollPeriod != null && w.PayrollPeriod.Status == upperStatus) ||
                (w.PayrollPeriod != null && w.PayrollPeriod.Timesheets.Any(t => t.Status == upperStatus)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<WorkScheduleWeek>> GetAvailableWeeksForBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return _context.WorkScheduleWeeks
            .AsNoTracking()
            .Where(w => w.BranchId == branchId)
            .OrderByDescending(w => w.WeekStartDate)
            .ThenByDescending(w => w.ScheduleWeekId)
            .ToListAsync(cancellationToken);
    }
}
