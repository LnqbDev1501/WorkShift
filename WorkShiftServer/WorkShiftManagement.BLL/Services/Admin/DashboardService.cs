using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepo _dashboardRepo;

    public DashboardService(IDashboardRepo dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    public async Task<DashboardResponse> GetAsync()
    {
        var timeZone = GetVietnamTimeZone();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var today = DateOnly.FromDateTime(now);
        var localStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var roleCounts = await _dashboardRepo.Users
            .GroupBy(user => user.Role.RoleName.ToUpper())
            .Select(group => new { RoleName = group.Key, Count = group.Count() })
            .ToListAsync();

        var attendanceStatuses = await _dashboardRepo.Attendances
            .Where(attendance => attendance.ShiftStartAtSnapshot >= utcStart
                && attendance.ShiftStartAtSnapshot < utcEnd)
            .GroupBy(attendance => attendance.Status)
            .Select(group => new AttendanceStatusResponse { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync();

        var recentPayrollPeriods = await _dashboardRepo.PayrollPeriods
            .OrderByDescending(period => period.EndDate)
            .ThenByDescending(period => period.PayrollPeriodId)
            .Take(3)
            .Select(period => new RecentPayrollPeriodResponse
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

        return new DashboardResponse
        {
            TotalUsers = roleCounts.Sum(role => role.Count),
            ManagerCount = roleCounts.FirstOrDefault(role => role.RoleName == "MANAGER")?.Count ?? 0,
            AccountantCount = roleCounts.FirstOrDefault(role => role.RoleName == "ACCOUNTANT")?.Count ?? 0,
            StaffCount = roleCounts.FirstOrDefault(role => role.RoleName == "STAFF")?.Count ?? 0,
            TotalRegions = await _dashboardRepo.Regions.CountAsync(),
            TotalBranches = await _dashboardRepo.Branches.CountAsync(),
            ActiveBranches = await _dashboardRepo.Branches.CountAsync(branch => branch.Status == "ACTIVE"),
            PendingPayrolls = await _dashboardRepo.Payrolls.CountAsync(payroll => payroll.Status == "SUBMITTED"),
            TodayAttendance = new AttendanceSummaryResponse
            {
                Date = today,
                Total = attendanceStatuses.Sum(item => item.Count),
                ByStatus = attendanceStatuses
            },
            RecentPayrollPeriods = recentPayrollPeriods
        };
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
