namespace WorkShiftManagement.DAL.Models.Admin;

public sealed class DashboardData
{
    public int TotalUsers { get; set; }
    public int ManagerCount { get; set; }
    public int AccountantCount { get; set; }
    public int StaffCount { get; set; }
    public int TotalRegions { get; set; }
    public int TotalBranches { get; set; }
    public int ActiveBranches { get; set; }
    public int PendingPayrolls { get; set; }
    public List<AttendanceStatusData> TodayAttendance { get; set; } = [];
    public List<RecentPayrollPeriodData> RecentPayrollPeriods { get; set; } = [];
}

public sealed class AttendanceStatusData
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public sealed class RecentPayrollPeriodData
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
