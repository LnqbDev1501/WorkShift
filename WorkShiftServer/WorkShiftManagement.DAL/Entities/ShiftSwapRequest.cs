using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ShiftSwapRequest
{
    public int SwapRequestId { get; set; }

    public Guid RequesterId { get; set; }

    public Guid TargetUserId { get; set; }

    public int SourceAssignmentId { get; set; }

    public int TargetAssignmentId { get; set; }

    public string? Reason { get; set; }

    public Guid? RespondedBy { get; set; }

    public DateTime? RespondedAt { get; set; }

    public string? ResponseNote { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Requester { get; set; } = null!;

    public virtual User? RespondedByNavigation { get; set; }

    public virtual ShiftAssignment SourceAssignment { get; set; } = null!;

    public virtual ShiftAssignment TargetAssignment { get; set; } = null!;

    public virtual User TargetUser { get; set; } = null!;
}
