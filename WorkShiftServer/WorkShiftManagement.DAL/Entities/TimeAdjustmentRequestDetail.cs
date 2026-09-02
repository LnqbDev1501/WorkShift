using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class TimeAdjustmentRequestDetail
{
    public int RequestId { get; set; }

    public int ShiftId { get; set; }

    public DateTime RequestedTime { get; set; }

    public virtual ICollection<Attendance> AttendanceEarlyLeaveRequests { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceLateArrivalRequests { get; set; } = new List<Attendance>();

    public virtual EmployeeRequest Request { get; set; } = null!;

    public virtual WorkShift Shift { get; set; } = null!;
}
