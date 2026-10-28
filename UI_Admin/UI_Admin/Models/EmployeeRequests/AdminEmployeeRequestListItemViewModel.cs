namespace UI_Admin.Models.EmployeeRequests;

public class AdminEmployeeRequestListItemViewModel
{
    public int RequestId { get; set; }

    public Guid UserId { get; set; }

    public string EmployeeFullName { get; set; } = string.Empty;

    public string EmployeeEmail { get; set; } = string.Empty;

    public string EmployeePhone { get; set; } = string.Empty;

    public string? EmployeeAvatarUrl { get; set; }

    public int BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewerFullName { get; set; }
    public AdminSalaryIncreaseDetailViewModel? SalaryIncreaseDetail { get; set; }

    public AdminRequestShiftSummaryViewModel? ShiftSummary { get; set; }
}

public class AdminRequestShiftSummaryViewModel
{
    public int TotalShifts { get; set; }

    public string? PrimaryShiftName { get; set; }

    public DateTime? PrimaryScheduledStartAt { get; set; }

    public DateTime? PrimaryScheduledEndAt { get; set; }

    public DateTime? RequestedTime { get; set; }
}
