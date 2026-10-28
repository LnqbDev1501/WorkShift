using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Notification.Request;

public sealed class PersonalNotificationListQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    [RegularExpression("^(all|unread|read)$", ErrorMessage = "Filter must be all, unread, or read.")]
    public string Filter { get; set; } = "all";

    [RegularExpression("^(CreatedAt|Title)$", ErrorMessage = "SortBy must be CreatedAt or Title.")]
    public string SortBy { get; set; } = "CreatedAt";

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be asc or desc.")]
    public string SortOrder { get; set; } = "desc";

    public string? Search { get; set; }
}
