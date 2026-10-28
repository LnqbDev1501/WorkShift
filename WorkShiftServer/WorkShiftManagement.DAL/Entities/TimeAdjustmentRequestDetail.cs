namespace WorkShiftManagement.DAL.Entities;

public partial class TimeAdjustmentRequestDetail
{
    public int RequestId { get; set; }

    public DateTime RequestedTime { get; set; }

    public int AssignmentId { get; set; }

    public virtual ShiftAssignment Assignment { get; set; } = null!;

    public virtual ICollection<Attendance> AttendanceEarlyLeaveRequests { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceLateArrivalRequests { get; set; } = new List<Attendance>();

    public virtual EmployeeRequest Request { get; set; } = null!;
}
