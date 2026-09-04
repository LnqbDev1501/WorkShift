namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class NotificationListQuery
{
    public string? Search { get; set; }

    public string? Type { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; } = "CreatedAt";

    public string SortOrder { get; set; } = "desc";
}
