using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public class StaffEmployeeRequestListItemResponse
{
    public int RequestId { get; set; }

    public Guid UserId { get; set; }

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
}
