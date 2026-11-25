namespace UI_Admin.Models.Admin.AttendancePenaltyRule;

public sealed class AttendancePenaltyLevelViewModel
{
    public int PenaltyLevelId { get; set; }
    public int PenaltyRuleId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public int FromMinutes { get; set; }
    public int? ToMinutes { get; set; }
    public decimal PenaltyAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
