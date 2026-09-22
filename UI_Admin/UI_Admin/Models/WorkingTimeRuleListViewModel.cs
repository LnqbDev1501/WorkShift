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
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool CanEdit => Status == "DRAFT";
    public bool CanCancel => Status == "PUBLISH";
    public string StatusCssClass => Status switch
    {
        "DRAFT" => "draft",
        "PUBLISH" => "publish",
        "ACTIVE" => "active",
        "CANCEL" => "cancel",
        _ => "inactive"
    };
    public string StatusLabel => Status switch
    {
        "DRAFT" => "Draft",
        "PUBLISH" => "Publish",
        "ACTIVE" => "Active",
        "INACTIVE" => "Inactive",
        "CANCEL" => "Cancelled",
        _ => Status
    };
}
