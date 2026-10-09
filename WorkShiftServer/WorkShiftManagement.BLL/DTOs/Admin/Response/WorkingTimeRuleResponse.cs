namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public sealed class WorkingTimeRuleResponse
{
    public int WorkingTimeRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public decimal MaxDailyHours { get; set; }
    public decimal MaxWeeklyHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
