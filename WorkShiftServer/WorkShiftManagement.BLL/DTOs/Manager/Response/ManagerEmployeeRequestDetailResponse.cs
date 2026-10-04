using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class ManagerEmployeeRequestDetailResponse
{
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

    public DateTime UpdatedAt { get; set; }

    // Review information
    public Guid? ReviewedBy { get; set; }

    public string? ReviewerFullName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    // Applied Request Limit Rule (if any)
    public ManagerAppliedRequestLimitRuleResponse? AppliedRule { get; set; }

    // Type-specific details
    public List<ManagerLeaveRequestAssignmentResponse>? LeaveAssignments { get; set; }

    public ManagerTimeAdjustmentDetailResponse? TimeAdjustmentDetail { get; set; }
}

public class ManagerAppliedRequestLimitRuleResponse
{
    public int RuleId { get; set; }

    public string RequestType { get; set; } = null!;

    public int MaxRequestsPerMonth { get; set; }
}

public class ManagerLeaveRequestAssignmentResponse
{
    public int LeaveRequestAssignmentId { get; set; }

    public int AssignmentId { get; set; }

    public string AssignmentStatus { get; set; } = null!;

    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = null!;

    public string? ShiftTemplateName { get; set; }

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }

    public string? BranchName { get; set; }
}

public class ManagerTimeAdjustmentDetailResponse
{
    public int AssignmentId { get; set; }

    public DateTime RequestedTime { get; set; }

    public string AssignmentStatus { get; set; } = null!;

    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = null!;

    public string? ShiftTemplateName { get; set; }

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }

    public string? BranchName { get; set; }
}
