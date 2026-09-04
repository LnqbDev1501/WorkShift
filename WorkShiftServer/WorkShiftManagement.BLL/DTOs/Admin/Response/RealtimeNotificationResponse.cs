namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class RealtimeNotificationResponse
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
