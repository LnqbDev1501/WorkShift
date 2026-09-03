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
    public int PendingRequests { get; set; }
    public AttendanceSummaryResponse TodayAttendance { get; set; } = new();
    public List<RecentPayrollResponse> RecentPayrolls { get; set; } = [];
    public List<RecentRequestResponse> RecentRequests { get; set; } = [];
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

public sealed class RecentPayrollResponse
{
    public int PayrollId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string PeriodName { get; set; } = null!;
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}

public sealed class RecentRequestResponse
{
    public int RequestId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string RequestType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
