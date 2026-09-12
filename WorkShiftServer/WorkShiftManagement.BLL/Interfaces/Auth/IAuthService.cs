using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;

namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<Guid> CreateUserAndSendInviteAsync(string email);
    Task<string?> ResendSetupLinkAsync(Guid userId, string email);
    Task DeleteUserAsync(Guid userId);
    Task BanUserAsync(Guid userId);
    Task UnbanUserAsync(Guid userId);
    Task<Guid> SetPasswordAsync(string accessToken, string password);
    Task UpdateEmailAsync(Guid userId, string email);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
