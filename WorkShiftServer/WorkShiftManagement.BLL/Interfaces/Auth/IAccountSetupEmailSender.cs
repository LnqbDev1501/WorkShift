namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAccountSetupEmailSender
{
    Task SendAsync(string recipientEmail, string setupLink);
}
