using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response;

namespace WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Response;

public sealed class PayrollPeriodManagementResponse
{
    public int PayrollPeriodId { get; set; }
    public int? ScheduleWeekId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovalDeadlineAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int ExpectedEmployeeCount { get; set; }
    public int LockedTimesheetCount { get; set; }
    public int TotalPayrolls { get; set; }
    public int RejectedPayrollCount { get; set; }
    public int PendingCorrectionCount { get; set; }
    public decimal TotalNetSalary { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanOpen { get; set; }
    public List<PayrollResponse> Payrolls { get; set; } = [];
}

public sealed class AvailableScheduleWeekResponse
{
    public int ScheduleWeekId { get; set; }
    public int BranchId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed record PayrollPeriodReportResponse(byte[] Content, string FileName);
