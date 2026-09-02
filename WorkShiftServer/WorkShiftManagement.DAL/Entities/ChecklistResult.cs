using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class ChecklistResult
{
    public int ResultId { get; set; }

    public int ChecklistItemId { get; set; }

    public Guid UserId { get; set; }

    public bool IsCompleted { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Note { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ChecklistItem ChecklistItem { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
