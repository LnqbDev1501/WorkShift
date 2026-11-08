using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<Guid> CreateUserAndSendInviteAsync(string email);
    Task<Guid> CreateUserWithPasswordAsync(string email, string password);
    Task<string?> ResendSetupLinkAsync(Guid userId, string email);
    Task DeleteUserAsync(Guid userId);
    Task BanUserAsync(Guid userId);
    Task UnbanUserAsync(Guid userId);
    Task<Guid> SetPasswordAsync(Guid userId, string currentPassword, string password);
    Task<Guid> SetPasswordWithTokenAsync(string accessToken, string password);
    Task UpdateEmailAsync(Guid userId, string email);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(string refreshToken);
    Task SendPasswordRecoveryAsync(string email);

    // Staff setup flow (custom 3-min token)
    Task<Guid> CreateStaffUserAsync(string email);
    Task<bool> HasPasswordAsync(Guid userId);
    Task<Guid> SetStaffPasswordAsync(string token, string password);
    Task<User?> GetStaffSetupUserAsync(Guid userId);
    Task SetStaffPasswordAndActivateAsync(Guid userId, string password);
}
