namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<Guid> CreateUserAndSendInviteAsync(string email);
    Task ResendSetupLinkAsync(Guid userId, string email);
    Task DeleteUserAsync(Guid userId);
    Task<Guid> SetPasswordAsync(string accessToken, string password);
}
