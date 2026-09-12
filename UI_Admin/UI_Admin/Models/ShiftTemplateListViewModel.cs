using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class ShiftTemplateListQueryViewModel
{
    [Range(1, 20, ErrorMessage = "Page must be a whole number between 1 and 20.")]
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
}

public sealed class ShiftTemplateListViewModel
{
    public ShiftTemplateListQueryViewModel Query { get; set; } = new();
    public ShiftTemplatePage Result { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int LastPage => Math.Min(20, Result.TotalPages);
    public int FirstItem => Result.Items.Count == 0 ? 0 : (Result.PageNumber - 1) * Result.PageSize + 1;
    public int LastItem => Result.Items.Count == 0 ? 0 : FirstItem + Result.Items.Count - 1;
}

public sealed class ShiftTemplatePage
{
    public List<ShiftTemplateListItem> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ShiftTemplateListItem
{
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
}
