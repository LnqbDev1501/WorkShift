using System;
using System.Collections.Generic;

namespace UI_Admin.Models.Staff.Requests;

public class StaffEmployeeRequestListItemViewModel
{
    public int RequestId { get; set; }
    public Guid UserId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewerFullName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public StaffRequestShiftSummaryViewModel? ShiftSummary { get; set; }
}

public class StaffRequestShiftSummaryViewModel
{
    public int TotalShifts { get; set; }
    public string? PrimaryShiftName { get; set; }
    public DateTime? PrimaryScheduledStartAt { get; set; }
    public DateTime? PrimaryScheduledEndAt { get; set; }
    public DateTime? RequestedTime { get; set; }
}

public class StaffEmployeeRequestListViewModel
{
    public IReadOnlyList<StaffEmployeeRequestListItemViewModel> Items { get; set; } = new List<StaffEmployeeRequestListItemViewModel>();

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    // Filters
    public string? Reason { get; set; }
    public string? RequestType { get; set; }
    public string? Status { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }

    // Messages
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Reason) ||
        (!string.IsNullOrWhiteSpace(RequestType) && !RequestType.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("ALL", StringComparison.OrdinalIgnoreCase)) ||
        !string.IsNullOrWhiteSpace(FromDate) ||
        !string.IsNullOrWhiteSpace(ToDate);
}

public class StaffEmployeeRequestDetailViewModel
{
    public int RequestId { get; set; }
    public Guid UserId { get; set; }
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

    public StaffAppliedRequestLimitRuleViewModel? AppliedRule { get; set; }
    public IReadOnlyList<StaffLeaveRequestAssignmentViewModel> LeaveAssignments { get; set; } = new List<StaffLeaveRequestAssignmentViewModel>();
    public StaffTimeAdjustmentDetailViewModel? TimeAdjustmentDetail { get; set; }

    public string? ReturnUrl { get; set; }
}

public class StaffAppliedRequestLimitRuleViewModel
{
    public int RuleId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int MaxRequestsPerMonth { get; set; }
}

public class StaffLeaveRequestAssignmentViewModel
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

public class StaffTimeAdjustmentDetailViewModel
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

public class StaffEligibleShiftViewModel
{
    public int AssignmentId { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string? ShiftTemplateName { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string AssignmentStatus { get; set; } = string.Empty;
}
