namespace UI_Admin.Models;

public sealed class ShiftTemplateDetailViewModel
{
    public int ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsInactive => string.Equals(Status, "INACTIVE", StringComparison.OrdinalIgnoreCase);
    public double TotalMinutes => (EndTime - StartTime).TotalMinutes;
    public static string Duration(double minutes) => $"{(int)minutes / 60} hrs {(int)minutes % 60:00} mins";
}
