using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;

namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IProfileService
{
    Task<ProfileResponse?> GetMyProfileAsync(ClaimsPrincipal user);
    Task<ProfileResponse> UpdateMyProfileAsync(ClaimsPrincipal user, UpdateMyProfileRequest request);
    Task<string> UpdateMyAvatarAsync(ClaimsPrincipal user, IFormFile? avatarFile);
    Task<PendingEmailChange> RequestEmailChangeAsync(ClaimsPrincipal user, string newEmail);
    Task<ProfileResponse> ConfirmEmailChangeAsync(Guid userId, string newEmail);
}

public sealed class PendingEmailChange
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
}
