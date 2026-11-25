using System;
using System.Collections.Generic;

namespace UI_Admin.Models.EmployeeRequests;

public class AdminEmployeeRequestDetailViewModel
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

    // Review information
    public Guid? ReviewedBy { get; set; }

    public string? ReviewerFullName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    // Applied Request Limit Rule (if any)
    public AdminAppliedRequestLimitRuleViewModel? AppliedRule { get; set; }

    // Type-specific details
    public AdminSalaryIncreaseDetailViewModel? SalaryIncreaseDetail { get; set; }

    public List<AdminLeaveRequestAssignmentViewModel>? LeaveAssignments { get; set; }

    public AdminTimeAdjustmentDetailViewModel? TimeAdjustmentDetail { get; set; }

    // History
    public IReadOnlyList<AdminEmployeeRequestHistoryViewModel> History { get; set; } = [];

    public string? HistoryErrorMessage { get; set; }

    // UI state
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }
}

public class AdminEmployeeRequestHistoryViewModel
{
    public long AuditLogId { get; set; }
    public int RequestId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? RoleName { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? ReviewNote { get; set; }
}

public class AdminAppliedRequestLimitRuleViewModel
{
    public int RuleId { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public int MaxRequestsPerMonth { get; set; }

    public int MinimumAdvanceDays { get; set; }

    public short? SubmissionDeadlineDayOfWeek { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class AdminLeaveRequestAssignmentViewModel
{
    public int LeaveRequestAssignmentId { get; set; }

    public int AssignmentId { get; set; }

    public string AssignmentStatus { get; set; } = string.Empty;

    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public string? ShiftTemplateName { get; set; }

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }

    public string? BranchName { get; set; }
}

public class AdminTimeAdjustmentDetailViewModel
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

public class AdminSalaryIncreaseDetailViewModel
{
    public int CurrentContractId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
}

