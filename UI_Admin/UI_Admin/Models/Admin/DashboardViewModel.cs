using System.Globalization;

namespace UI_Admin.Models;

public sealed class DashboardViewModel
{
    public DashboardDataViewModel? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    public static string Percent(int count, int total) => (total <= 0 ? 0 : Math.Clamp(100d * count / total, 0, 100)).ToString("0.0", CultureInfo.InvariantCulture);
    public static string Label(string status) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace('_', ' ').ToLowerInvariant());
    public static string StatusClass(string status) => status.ToUpperInvariant() switch
    {
        "ON_TIME" or "PRESENT" or "APPROVED" => "primary",
        "LATE" or "LATE_ARRIVAL" or "SUBMITTED" => "pending",
        "ABSENT" or "MISSED" or "REJECTED" or "REVISION_REQUIRED" => "danger",
        _ => "neutral"
    };
}

public sealed class DashboardDataViewModel
{
    public int TotalUsers { get; set; }
    public int ManagerCount { get; set; }
    public int AccountantCount { get; set; }
    public int StaffCount { get; set; }
    public int TotalRegions { get; set; }
    public int TotalBranches { get; set; }
    public int ActiveBranches { get; set; }
    public int PendingPayrolls { get; set; }
    public AttendanceSummaryViewModel TodayAttendance { get; set; } = new();
    public List<RecentPayrollPeriodViewModel> RecentPayrollPeriods { get; set; } = [];
}

public sealed class AttendanceSummaryViewModel
{
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public List<AttendanceStatusViewModel> ByStatus { get; set; } = [];
}

public sealed class AttendanceStatusViewModel
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public sealed class RecentPayrollPeriodViewModel
{
    public int PayrollPeriodId { get; set; }
    public string BranchName { get; set; } = "";
    public string PeriodCode { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "";
    public int TotalPayrolls { get; set; }
}
