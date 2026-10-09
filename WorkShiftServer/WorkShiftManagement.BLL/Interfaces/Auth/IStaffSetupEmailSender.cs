namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IStaffSetupEmailSender
{
    Task SendAsync(string recipientEmail, string setupLink);
}