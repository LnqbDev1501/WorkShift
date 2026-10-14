using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmployeeRequestListItemResponse
{
    public ManagerSalaryIncreaseDetailResponse? SalaryIncreaseDetail { get; set; }

    public int RequestId { get; set; }

    public Guid UserId { get; set; }

    public string EmployeeFullName { get; set; } = null!;

    public string EmployeeEmail { get; set; } = null!;

    public string EmployeePhone { get; set; } = null!;

    public string? EmployeeAvatarUrl { get; set; }

    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public string BranchCode { get; set; } = null!;

    public string RequestType { get; set; } = null!;

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid? ReviewedBy { get; set; }

    public string? ReviewerFullName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }
    public ManagerRequestShiftSummaryResponse? ShiftSummary { get; set; }
}

public class ManagerRequestShiftSummaryResponse
{
    public int TotalShifts { get; set; }
    public string? PrimaryShiftName { get; set; }
    public DateTime? PrimaryScheduledStartAt { get; set; }
    public DateTime? PrimaryScheduledEndAt { get; set; }
    public DateTime? RequestedTime { get; set; }
}
