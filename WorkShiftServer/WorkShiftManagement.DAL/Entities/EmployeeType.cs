using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class EmployeeType
{
    public int EmployeeTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ShiftEmployeeRequirement> ShiftEmployeeRequirements { get; set; } = new List<ShiftEmployeeRequirement>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
