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
    public int PendingRequests { get; set; }
    public List<AttendanceStatusData> TodayAttendance { get; set; } = [];
    public List<RecentPayrollData> RecentPayrolls { get; set; } = [];
    public List<RecentRequestData> RecentRequests { get; set; } = [];
}

public sealed class AttendanceStatusData
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public sealed class RecentPayrollData
{
    public int PayrollId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string PeriodName { get; set; } = null!;
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}

public sealed class RecentRequestData
{
    public int RequestId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string RequestType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
