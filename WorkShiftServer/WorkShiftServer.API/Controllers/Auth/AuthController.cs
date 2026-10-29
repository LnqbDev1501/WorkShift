using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.DAL.Data;
using WorkShiftServer.API.Services;

namespace WorkShiftServer.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly StaffSetupTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;

    public AuthController(
        IAuthService authService,
        StaffSetupTokenService tokenService,
        ILogger<AuthController> logger,
        AppDbContext context)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var login = await _authService.LoginAsync(request);
            var user = await _context.Users.AsNoTracking()
                .Where(item => item.UserId == login.UserId && item.Status == "ACTIVE" && item.DeletedAt == null)
                .Select(item => new { item.FullName, Role = item.Role.RoleName })
                .SingleOrDefaultAsync();
            if (user == null)
                return Unauthorized(new { message = "Your WorkShift account is inactive or unavailable." });

            login.FullName = user.FullName;
            login.Role = user.Role.ToUpperInvariant();
            return Ok(login);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var login = await _authService.RefreshAsync(request.RefreshToken);
            var user = await _context.Users.AsNoTracking()
                .Where(item => item.UserId == login.UserId && item.Status == "ACTIVE" && item.DeletedAt == null)
                .Select(item => new { item.FullName, Role = item.Role.RoleName })
                .SingleOrDefaultAsync();
            if (user == null)
                return Unauthorized(new { message = "Your WorkShift account is inactive or unavailable." });

            login.FullName = user.FullName;
            login.Role = user.Role.ToUpperInvariant();
            return Ok(login);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Unauthorized(new { message = "Your session has expired. Please sign in again." });
        }
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var email = request.Email.Trim();
            var hasActiveAccount = await _context.Users.AsNoTracking().AnyAsync(item =>
                item.Email == email && item.Status == "ACTIVE" && item.DeletedAt == null);
            if (hasActiveAccount)
                await _authService.SendPasswordRecoveryAsync(email);
        }
        catch (Exception exception)
        {
            // Return the same response to avoid exposing account existence or mail delivery failures.
            _logger.LogError(exception, "Unable to process password recovery request");
        }

        return NoContent();
    }

    [HttpPost("set-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        try
        {
            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var guid))
                throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
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
            when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
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
            _logger.LogError(exception, "Supabase Auth failed while setting a password");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
    }

    [HttpPost("staff-setup/validate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateStaffSetupToken([FromBody] ValidateStaffSetupTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        var userId = _tokenService.ReadToken(request.Token);
        if (userId == null)
        {
            return BadRequest(new { message = "The setup link is invalid or has expired." });
        }

        // Verify user exists and is active or pending
        var user = await _context.Users.AsNoTracking()
            .Where(u => u.UserId == userId.Value && (u.Status == "ACTIVE" || u.Status == "PENDING") && u.DeletedAt == null)
            .Select(u => new { u.UserId, u.Email, u.FullName })
            .SingleOrDefaultAsync();

        if (user == null)
        {
            return BadRequest(new { message = "The associated account is no longer available." });
        }

        return Ok(new { userId = user.UserId, email = user.Email, fullName = user.FullName });
    }

    [HttpPost("staff-setup/set-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SetStaffPassword([FromBody] SetStaffPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        var userId = _tokenService.ReadToken(request.Token);
        if (userId == null)
        {
            return BadRequest(new { message = "The setup link is invalid or has expired." });
        }

        try
        {
            // Use Supabase admin API to set password
            await _authService.SetStaffPasswordAsync(userId.Value.ToString(), request.Password);

            // Update local user status if needed
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId.Value);
            if (user != null)
            {
                user.HasPassword = true;
                user.UpdatedAt = DateTime.UtcNow;
                if (user.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    user.Status = "ACTIVE";
                }
                else if (!user.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The current account status does not allow activation.");
                }
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return BadRequest(new { message = "The setup link is invalid or has expired." });
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
            _logger.LogError(exception, "Supabase Auth failed while setting staff password");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
    }

    public sealed record ValidateStaffSetupTokenRequest(string Token);
    public sealed record SetStaffPasswordRequest(string Token, string Password);
}
