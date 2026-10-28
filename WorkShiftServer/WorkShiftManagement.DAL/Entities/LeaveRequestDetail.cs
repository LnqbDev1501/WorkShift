namespace WorkShiftManagement.DAL.Entities;

public partial class LeaveRequestDetail
{
    public int RequestId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalDays { get; set; }

    public virtual EmployeeRequest Request { get; set; } = null!;
}
