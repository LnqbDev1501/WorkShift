using System.Security.Claims;
using WorkShiftManagement.BLL.DTOs.Auth.Response;

namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IProfileService
{
    Task<ProfileResponse?> GetMyProfileAsync(ClaimsPrincipal user);
}