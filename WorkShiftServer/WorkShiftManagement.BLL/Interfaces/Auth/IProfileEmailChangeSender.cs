namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IProfileEmailChangeSender
{
    Task SendAsync(string recipientEmail, string fullName, string verifyLink, int expiresMinutes);
}
