using System;
using System.Collections.Generic;

namespace UI_Admin.Models.Manager.Requests;

public class ManagerEmployeeRequestDetailViewModel
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
    public DateTime UpdatedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewerFullName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public ManagerAppliedRequestLimitRuleViewModel? AppliedRule { get; set; }
    public IReadOnlyList<ManagerLeaveRequestAssignmentViewModel> LeaveAssignments { get; set; } = new List<ManagerLeaveRequestAssignmentViewModel>();
    public ManagerTimeAdjustmentDetailViewModel? TimeAdjustmentDetail { get; set; }
    public IReadOnlyList<ManagerEmployeeRequestHistoryViewModel> History { get; set; } = new List<ManagerEmployeeRequestHistoryViewModel>();

    public string? ReturnUrl { get; set; }
}

public class ManagerAppliedRequestLimitRuleViewModel
{
    public int RuleId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int MaxRequestsPerMonth { get; set; }
}

public class ManagerLeaveRequestAssignmentViewModel
{
    public int LeaveRequestAssignmentId { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public string? ShiftTemplateName { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string? BranchName { get; set; }
}

public class ManagerTimeAdjustmentDetailViewModel
{
    public int AssignmentId { get; set; }
    public DateTime RequestedTime { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string? ShiftTemplateName { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string? BranchName { get; set; }
}

public class ManagerEmployeeRequestHistoryViewModel
{
    public long AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? FullName { get; set; }
    public string? RoleName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Note { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
