using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ShiftAssignment
{
    public int AssignmentId { get; set; }

    public int ShiftId { get; set; }

    public Guid UserId { get; set; }

    public string AssignmentSource { get; set; } = null!;

    public Guid AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public string Status { get; set; } = null!;

    public Guid? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? ShiftRequirementId { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual Attendance? Attendance { get; set; }

    public virtual User? CancelledByNavigation { get; set; }

    public virtual ICollection<LeaveRequestAssignment> LeaveRequestAssignments { get; set; } = new List<LeaveRequestAssignment>();

    public virtual ICollection<PayslipWorkDetail> PayslipWorkDetails { get; set; } = new List<PayslipWorkDetail>();

    public virtual WorkShift Shift { get; set; } = null!;

    public virtual ShiftEmployeeRequirement? ShiftEmployeeRequirement { get; set; }

    public virtual ICollection<ShiftSwapRequest> ShiftSwapRequestSourceAssignments { get; set; } = new List<ShiftSwapRequest>();

    public virtual ICollection<ShiftSwapRequest> ShiftSwapRequestTargetAssignments { get; set; } = new List<ShiftSwapRequest>();

    public virtual ICollection<TimeAdjustmentRequestDetail> TimeAdjustmentRequestDetails { get; set; } = new List<TimeAdjustmentRequestDetail>();

    public virtual User User { get; set; } = null!;
}
