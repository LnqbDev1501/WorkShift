namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;

public sealed class PublicHolidayListRequest
{
    public int PageNumber { get; set; } = 1;
    public int? Year { get; set; }
    public int? Month { get; set; }
    public string? Search { get; set; }

    // Admin can request both active and soft-deleted records.
    // Omitted/false: active records only.
    public bool? IsActive { get; set; }
    public bool IncludeInactive { get; set; } = false;
}

