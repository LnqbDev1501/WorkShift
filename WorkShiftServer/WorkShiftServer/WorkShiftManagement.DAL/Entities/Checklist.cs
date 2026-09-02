using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Checklist
{
    public int ChecklistId { get; set; }

    public int ShiftId { get; set; }

    public string ChecklistName { get; set; } = null!;

    public string? Description { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual WorkShift Shift { get; set; } = null!;
}
