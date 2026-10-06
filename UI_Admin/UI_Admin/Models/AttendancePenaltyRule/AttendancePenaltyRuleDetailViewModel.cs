namespace UI_Admin.Models.AttendancePenaltyRule;

public sealed class AttendancePenaltyRuleDetailViewModel
{
    public int PenaltyRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? PublishedByName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AttendancePenaltyLevelViewModel> PenaltyLevels { get; set; } = new();
}
