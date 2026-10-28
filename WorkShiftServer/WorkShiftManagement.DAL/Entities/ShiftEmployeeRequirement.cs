namespace WorkShiftManagement.DAL.Entities;

public partial class ShiftEmployeeRequirement
{
    public int ShiftRequirementId { get; set; }

    public int ShiftId { get; set; }

    public int EmployeeTypeId { get; set; }

    public int RequiredCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual EmployeeType EmployeeType { get; set; } = null!;

    public virtual WorkShift Shift { get; set; } = null!;

    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
}
