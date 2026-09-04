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

        var data = await _dashboardRepo.GetAsync(utcStart, utcEnd);

        return new DashboardResponse
        {
            TotalUsers = data.TotalUsers,
            ManagerCount = data.ManagerCount,
            AccountantCount = data.AccountantCount,
            StaffCount = data.StaffCount,
            TotalRegions = data.TotalRegions,
            TotalBranches = data.TotalBranches,
            ActiveBranches = data.ActiveBranches,
            PendingPayrolls = data.PendingPayrolls,
            PendingRequests = data.PendingRequests,
            TodayAttendance = new AttendanceSummaryResponse
            {
                Date = today,
                Total = data.TodayAttendance.Sum(item => item.Count),
                ByStatus = data.TodayAttendance.Select(item => new AttendanceStatusResponse
                {
                    Status = item.Status,
                    Count = item.Count
                }).ToList()
            },
            RecentPayrolls = data.RecentPayrolls.Select(item => new RecentPayrollResponse
            {
                PayrollId = item.PayrollId,
                EmployeeName = item.EmployeeName,
                BranchName = item.BranchName,
                PeriodName = item.PeriodName,
                NetSalary = item.NetSalary,
                Status = item.Status,
                UpdatedAt = item.UpdatedAt
            }).ToList(),
            RecentRequests = data.RecentRequests.Select(item => new RecentRequestResponse
            {
                RequestId = item.RequestId,
                EmployeeName = item.EmployeeName,
                BranchName = item.BranchName,
                RequestType = item.RequestType,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            }).ToList()
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
