namespace UI_Admin.Models.Manager;

public class RequestLimitRuleResponse
{
    public int RequestLimitRuleId { get; set; }
    public int BranchId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int MaxRequestsPerMonth { get; set; }
    public int MinimumAdvanceDays { get; set; }
    public short? SubmissionDeadlineDayOfWeek { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? SupersedesRuleId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
}


public sealed class RequestLimitRuleFilter
{
    public string? RequestType { get; set; }
    public string? Status { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue / 100)]
    public int PageNumber { get; set; } = 1;
}
public sealed class RequestLimitRulePage
{
    public List<RequestLimitRuleResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}
public sealed class RequestLimitRuleListViewModel
{
    public RequestLimitRuleFilter Filter { get; set; } = new();
    public RequestLimitRulePage? Page { get; set; }
    public string? ErrorMessage { get; set; }
}
public sealed class RequestLimitRuleDetailViewModel
{
    public int Id { get; set; }
    public RequestLimitRuleResponse? Rule { get; set; }
    public string? ErrorMessage { get; set; }
}
public sealed class RequestLimitRuleEditViewModel
{
    public int? Id { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Request type")]
    public string RequestType { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Maximum requests per month")]
    public int? MaxRequestsPerMonth { get; set; }
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Minimum advance notice (days)")]
    public int? MinimumAdvanceDays { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 7)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Submission deadline weekday")]
    public short? SubmissionDeadlineDayOfWeek { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Effective from")]
    public DateOnly? EffectiveFrom { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string Mode { get; set; } = "Create";
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public bool CanSave { get; set; } = true;
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public RequestLimitRuleResponse? Original { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public bool LockType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? ErrorMessage { get; set; }
}
public static class RequestRuleDisplay
{
    public static readonly string[] Types = ["LEAVE", "LATE_ARRIVAL", "EARLY_LEAVE", "SALARY_INCREASE"];
    public static readonly string[] Statuses = ["DRAFT", "SCHEDULED", "ACTIVE", "INACTIVE"];
    public static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
    public static string Label(string value) => ManagerDashboardViewModel.Label(value);
    public static string Tone(string status) => status switch { "ACTIVE" => "success", "SCHEDULED" => "accent", "INACTIVE" => "neutral", _ => "muted" };
    public static string Weekday(short? value) => value is >= 1 and <= 7 ? Weekdays[value.Value - 1] : "No weekday cutoff";
    public static string Icon(string type) => type switch { "LEAVE" => "event_busy", "LATE_ARRIVAL" => "schedule", "EARLY_LEAVE" => "logout", _ => "payments" };
    public static bool CanPublish(RequestLimitRuleResponse rule) => rule.Status == "DRAFT" && rule.EffectiveFrom >= Today && (rule.SubmissionDeadlineDayOfWeek == null || rule.RequestType == "SALARY_INCREASE");
}
