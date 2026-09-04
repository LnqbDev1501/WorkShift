namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class NotificationListResponse
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public List<string> Channels { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
