using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class EmployeeRequest
{
    public int RequestId { get; set; }

    public Guid UserId { get; set; }

    public int BranchId { get; set; }

    public string RequestType { get; set; } = null!;

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public int? AppliedRequestLimitRuleId { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual RequestLimitRule? AppliedRequestLimitRule { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<LeaveRequestAssignment> LeaveRequestAssignments { get; set; } = new List<LeaveRequestAssignment>();

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual ICollection<SalaryChangeRequest> SalaryChangeRequests { get; set; } = new List<SalaryChangeRequest>();

    public virtual SalaryIncreaseRequestDetail? SalaryIncreaseRequestDetail { get; set; }

    public virtual TimeAdjustmentRequestDetail? TimeAdjustmentRequestDetail { get; set; }

    public virtual User User { get; set; } = null!;
}
