using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class WorkingTimeRule
{
    public int WorkingTimeRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public int MaxDailyMinutes { get; set; }

    public int MaxWeeklyMinutes { get; set; }

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;
}
