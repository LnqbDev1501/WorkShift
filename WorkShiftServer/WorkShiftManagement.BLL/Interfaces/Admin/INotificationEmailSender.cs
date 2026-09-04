namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface INotificationEmailSender
{
    Task SendAsync(string recipientEmail, string title, string message);
}
