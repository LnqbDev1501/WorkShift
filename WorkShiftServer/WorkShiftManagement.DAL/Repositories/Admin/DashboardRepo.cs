using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class DashboardRepo : IDashboardRepo
{
    private readonly AppDbContext _context;

    public DashboardRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardData> GetAsync(DateTime todayStartUtc, DateTime tomorrowStartUtc)
    {
        var roleCounts = await _context.Users
            .AsNoTracking()
            .GroupBy(user => user.Role.RoleName.ToUpper())
            .Select(group => new { RoleName = group.Key, Count = group.Count() })
            .ToListAsync();

        var attendanceStatuses = await _context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.ShiftStartAtSnapshot >= todayStartUtc
                && attendance.ShiftStartAtSnapshot < tomorrowStartUtc)
            .GroupBy(attendance => attendance.Status)
            .Select(group => new AttendanceStatusData
            {
                Status = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToListAsync();

        var recentPayrollPeriods = await _context.PayrollPeriods
            .AsNoTracking()
            .OrderByDescending(period => period.EndDate)
            .ThenByDescending(period => period.PayrollPeriodId)
            .Take(3)
            .Select(period => new RecentPayrollPeriodData
            {
                PayrollPeriodId = period.PayrollPeriodId,
                BranchName = period.BranchNameSnapshot,
                PeriodCode = period.PeriodCode,
                PeriodName = period.PeriodName,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                Status = period.Status,
                TotalPayrolls = period.Payrolls.Count
            })
            .ToListAsync();

        return new DashboardData
        {
            TotalUsers = roleCounts.Sum(role => role.Count),
            ManagerCount = roleCounts.FirstOrDefault(role => role.RoleName == "MANAGER")?.Count ?? 0,
            AccountantCount = roleCounts.FirstOrDefault(role => role.RoleName == "ACCOUNTANT")?.Count ?? 0,
            StaffCount = roleCounts.FirstOrDefault(role => role.RoleName == "STAFF")?.Count ?? 0,
            TotalRegions = await _context.Regions.AsNoTracking().CountAsync(),
            TotalBranches = await _context.Branches.AsNoTracking().CountAsync(),
            ActiveBranches = await _context.Branches.AsNoTracking()
                .CountAsync(branch => branch.Status == "ACTIVE"),
            PendingPayrolls = await _context.Payrolls.AsNoTracking()
                .CountAsync(payroll => payroll.Status == "SUBMITTED"),
            TodayAttendance = attendanceStatuses,
            RecentPayrollPeriods = recentPayrollPeriods
        };
    }

}
