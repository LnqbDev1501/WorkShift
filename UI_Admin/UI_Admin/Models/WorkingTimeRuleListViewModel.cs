namespace UI_Admin.Models;

public sealed class WorkingTimeRuleListViewModel
{
    public List<WorkingTimeRuleListItemViewModel> Items { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public sealed class WorkingTimeRuleListItemViewModel
{
    public int WorkingTimeRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public decimal MaxDailyHours { get; set; }
    public decimal MaxWeeklyHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
