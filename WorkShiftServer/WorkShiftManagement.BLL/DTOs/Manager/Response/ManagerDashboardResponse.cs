using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public sealed class ManagerDashboardResponse
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TotalStaff { get; set; }
    public int ActiveStaff { get; set; }
    public int InactiveStaff { get; set; }
    public int TodayShifts { get; set; }
    public int TodayAssignments { get; set; }
    public AttendanceSummaryResponse TodayAttendance { get; set; } = new();
    public int PendingRequests { get; set; }
    public List<ManagerDashboardRequestCountResponse> PendingRequestsByType { get; set; } = [];
    public List<ManagerDashboardRecentRequestResponse> RecentRequests { get; set; } = [];
    public ManagerDashboardScheduleWeekResponse? CurrentScheduleWeek { get; set; }
}

public sealed class ManagerDashboardRequestCountResponse
{
    public string RequestType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ManagerDashboardRecentRequestResponse
{
    public int RequestId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ManagerDashboardScheduleWeekResponse
{
    public int ScheduleWeekId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationDeadline { get; set; }
}
