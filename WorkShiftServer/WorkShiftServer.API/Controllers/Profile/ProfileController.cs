using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;

namespace WorkShiftServer.API.Controllers.Profile;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService) => _profileService = profileService;

    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetMyProfile()
    {
        var profile = await _profileService.GetMyProfileAsync(User);
        if (profile == null)
        {
            return NotFound(new { message = "Profile not found." });
        }
        return Ok(profile);
    }
}