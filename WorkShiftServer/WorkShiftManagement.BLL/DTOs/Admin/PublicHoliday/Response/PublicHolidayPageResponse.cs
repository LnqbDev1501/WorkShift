namespace WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Response;

// Module-specific response to avoid duplicating an unknown existing PagedResult<T>.
// If your project has a shared PagedResult<T>, replace this type with it.
public sealed class PublicHolidayPageResponse
{
    public IReadOnlyList<PublicHolidayResponse> Items { get; set; } = Array.Empty<PublicHolidayResponse>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
