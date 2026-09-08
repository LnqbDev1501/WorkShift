namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public sealed class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int ManagerCount { get; set; }
    public int AccountantCount { get; set; }
    public int StaffCount { get; set; }
    public int TotalRegions { get; set; }
    public int TotalBranches { get; set; }
    public int ActiveBranches { get; set; }
    public int PendingPayrolls { get; set; }
    public AttendanceSummaryResponse TodayAttendance { get; set; } = new();
    public List<RecentPayrollPeriodResponse> RecentPayrollPeriods { get; set; } = [];
}

public sealed class AttendanceSummaryResponse
{
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public List<AttendanceStatusResponse> ByStatus { get; set; } = [];
}

public sealed class AttendanceStatusResponse
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public sealed class RecentPayrollPeriodResponse
{
    public int PayrollPeriodId { get; set; }
    public string BranchName { get; set; } = null!;
    public string PeriodCode { get; set; } = null!;
    public string PeriodName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = null!;
    public int TotalPayrolls { get; set; }
}
