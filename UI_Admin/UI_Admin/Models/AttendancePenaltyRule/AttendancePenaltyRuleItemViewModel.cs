namespace UI_Admin.Models.AttendancePenaltyRule;

public sealed class AttendancePenaltyRuleItemViewModel
{
    public int PenaltyRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int LateLevelsCount { get; set; }
    public int EarlyLeaveLevelsCount { get; set; }
    public decimal MinPenaltyAmount { get; set; }
    public decimal MaxPenaltyAmount { get; set; }
}
