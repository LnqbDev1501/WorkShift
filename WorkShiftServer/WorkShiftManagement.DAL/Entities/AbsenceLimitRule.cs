namespace WorkShiftManagement.DAL.Entities;

public partial class AbsenceLimitRule
{
    public long AbsenceLimitRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public string? Description { get; set; }

    public int MaxAbsenceCount { get; set; }

    public int Version { get; set; }

    public long? ReplaceId { get; set; }

    public string Status { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AbsenceLimitRule> InverseReplace { get; set; } = new List<AbsenceLimitRule>();

    public virtual AbsenceLimitRule? Replace { get; set; }

    public virtual ICollection<WorkScheduleWeek> WorkScheduleWeeks { get; set; } = new List<WorkScheduleWeek>();
}
