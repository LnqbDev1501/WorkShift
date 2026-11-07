using UI_Admin.Models;

namespace UI_Admin.Models.Manager;

public sealed class ManagerDashboardData
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TotalStaff { get; set; }
    public int ActiveStaff { get; set; }
    public int InactiveStaff { get; set; }
    public int TodayShifts { get; set; }
    public int TodayAssignments { get; set; }
    public AttendanceSummaryViewModel TodayAttendance { get; set; } = new();
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

public sealed class ManagerDashboardViewModel
{
    public ManagerDashboardData? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public static string Label(string value) => value.ToUpperInvariant() switch
    {
        "SHIFT_REGISTRATION" => "Shift Registration", "SHIFT_SWAP" => "Shift Swap",
        "LEAVE" or "LEAVE_REQUEST" => "Leave Request", "TIME_ADJUSTMENT" => "Time Adjustment",
        _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').ToLowerInvariant())
    };
    public static string Color(string status) => status.ToUpperInvariant() switch
    {
        "ON_TIME" or "PRESENT" or "APPROVED" or "ACTIVE" => "#0f8a70",
        "LATE" or "LATE_ARRIVAL" or "PENDING" => "#b77912",
        "ABSENT" or "MISSED" or "REJECTED" => "#cf5369", _ => "#6256db"
    };
    public static string VietnamTime(DateTime value) => (value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc))
        .AddHours(7).ToString("dd MMM yyyy, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
