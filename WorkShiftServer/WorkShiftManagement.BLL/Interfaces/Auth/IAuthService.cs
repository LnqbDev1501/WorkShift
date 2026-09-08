namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<Guid> CreateUserAndSendInviteAsync(string email);
    Task<string?> ResendSetupLinkAsync(Guid userId, string email);
    Task DeleteUserAsync(Guid userId);
    Task BanUserAsync(Guid userId);
    Task UnbanUserAsync(Guid userId);
    Task<Guid> SetPasswordAsync(string accessToken, string password);
}
