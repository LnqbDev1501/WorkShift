namespace UI_Admin.Models.Admin.AttendancePenaltyRule;

public sealed class AttendancePenaltyRuleListViewModel
{
    public IReadOnlyList<AttendancePenaltyRuleItemViewModel> Items { get; set; } = Array.Empty<AttendancePenaltyRuleItemViewModel>();
    public int TotalItems { get; set; }
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    // Metrics summary
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int ScheduledRules { get; set; }
    public int DraftRules { get; set; }
}
