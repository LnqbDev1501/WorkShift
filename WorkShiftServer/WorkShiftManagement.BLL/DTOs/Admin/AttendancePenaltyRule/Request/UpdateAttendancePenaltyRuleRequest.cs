namespace WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;

public sealed class UpdateAttendancePenaltyRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public List<CreateAttendancePenaltyLevelRequest>? PenaltyLevels { get; set; }
}
