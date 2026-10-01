using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.AttendanceRule;

public sealed class AttendanceRuleListQueryViewModel
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [StringLength(150)]
    public string? Search { get; set; }

    [RegularExpression("^(DRAFT|SCHEDULED|ACTIVE|CANCELLED)$")]
    public string? Status { get; set; }
}

public sealed class AttendanceRuleListViewModel
{
    public AttendanceRuleListQueryViewModel Query { get; set; } = new();
    public IReadOnlyList<AttendanceRuleListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int ScheduledRules { get; set; }
    public int DraftRules { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AttendanceRuleListItemViewModel
{
    public int AttendanceRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CheckInBeforeMinutes { get; set; }
    public int CheckInGraceMinutes { get; set; }
    public int CheckOutGraceMinutes { get; set; }
    public int CheckOutAfterMinutes { get; set; }
    public int MaxAttendanceDistanceMeters { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool CanEdit => Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase);
    public bool CanPublish => Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase);
    public bool CanCancel => Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase) ||
                             Status.Equals("SCHEDULED", StringComparison.OrdinalIgnoreCase);
}
