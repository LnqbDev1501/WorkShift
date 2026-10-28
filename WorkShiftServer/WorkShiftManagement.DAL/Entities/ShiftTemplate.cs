namespace WorkShiftManagement.DAL.Entities;

public partial class ShiftTemplate
{
    public int ShiftTemplateId { get; set; }

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}
