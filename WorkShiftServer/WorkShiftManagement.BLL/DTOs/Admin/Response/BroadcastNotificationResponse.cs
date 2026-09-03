namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class BroadcastNotificationResponse
{
    public int NotificationId { get; set; }

    public int RecipientCount { get; set; }

    public int InAppRecipientCount { get; set; }

    public int EmailSentCount { get; set; }

    public int EmailFailedCount { get; set; }
}
