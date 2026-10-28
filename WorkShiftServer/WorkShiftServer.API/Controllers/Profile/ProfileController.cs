using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftServer.API.Services;

namespace WorkShiftServer.API.Controllers.Profile;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IProfileEmailChangeSender _emailChangeSender;
    private readonly ProfileEmailChangeTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        IAuthService authService,
        IProfileEmailChangeSender emailChangeSender,
        ProfileEmailChangeTokenService tokenService,
        IConfiguration configuration,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _authService = authService;
        _emailChangeSender = emailChangeSender;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

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

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var accessToken = GetBearerToken();
        if (string.IsNullOrWhiteSpace(accessToken))
            return Unauthorized(new { message = "Your session has expired. Please sign in again." });

        try
        {
            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var guid))
                return Unauthorized(new { message = "Your session has expired. Please sign in again." });
            await _authService.SetPasswordAsync(guid, request.CurrentPassword, request.Password);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Unauthorized(new { message = "Your session has expired. Please sign in again." });
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            return BadRequest(new { message = "The new password was rejected. Please check the password requirements." });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while changing password");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
    }

    private string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();
        return null;
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> UpdateMyProfile([FromBody] UpdateMyProfileRequest request)
    {
        try
        {
            return Ok(await _profileService.UpdateMyProfileAsync(User, request));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyAvatar(IFormFile? avatarFile)
    {
        try
        {
            var avatarUrl = await _profileService.UpdateMyAvatarAsync(User, avatarFile);
            return Ok(new { avatarUrl });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("email/change-request")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeRequest request)
    {
        PendingEmailChange pending;
        try
        {
            pending = await _profileService.RequestEmailChangeAsync(User, request.Email);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }

        var verifyBaseUrl = _configuration["AdminUi:VerifyEmailUrl"];
        if (string.IsNullOrWhiteSpace(verifyBaseUrl))
        {
            _logger.LogError("Missing AdminUi:VerifyEmailUrl configuration.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Email verification is not configured. Please try again later." });
        }

        var token = _tokenService.CreateToken(pending.UserId, pending.NewEmail);
        var verifyLink = $"{verifyBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";
        var expiresMinutes = (int)ProfileEmailChangeTokenService.TokenLifetime.TotalMinutes;

        try
        {
            await _emailChangeSender.SendAsync(pending.NewEmail, pending.FullName, verifyLink, expiresMinutes);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to send email-change verification email.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"The verification email could not be sent: {exception.Message}" });
        }

        return Accepted(new { message = "A verification link has been sent to the new email address." });
    }

    [HttpPost("email/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ProfileResponse>> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        var verified = _tokenService.ReadToken(request.Token);
        if (verified == null)
        {
            return BadRequest(new { message = "The verification link is invalid or has expired." });
        }

        try
        {
            return Ok(await _profileService.ConfirmEmailChangeAsync(verified.Value.UserId, verified.Value.NewEmail));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while confirming email change.");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
    }
}
