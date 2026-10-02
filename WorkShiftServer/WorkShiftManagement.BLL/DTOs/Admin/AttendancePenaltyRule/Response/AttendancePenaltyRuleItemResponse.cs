namespace WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;

public sealed class AttendancePenaltyRuleItemResponse
{
    public int PenaltyRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
