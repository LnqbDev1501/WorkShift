using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class LeaveRequestAssignment
{
    public int LeaveRequestAssignmentId { get; set; }

    public int RequestId { get; set; }

    public int AssignmentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ShiftAssignment Assignment { get; set; } = null!;

    public virtual EmployeeRequest Request { get; set; } = null!;
}
