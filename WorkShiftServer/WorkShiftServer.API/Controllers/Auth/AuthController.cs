using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        AppDbContext context)
    {
        _authService = authService;
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
            var userId = await _authService.SetPasswordAsync(request.AccessToken, request.Password);
            var user = await _context.Users.SingleOrDefaultAsync(item => item.UserId == userId)
                ?? throw new KeyNotFoundException("No matching user profile was found.");
            if (user.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
            {
                user.Status = "ACTIVE";
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (!user.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The current account status does not allow activation.");
            }
            return NoContent();
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return BadRequest(new { message = "The password setup link is invalid or has expired." });
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
}
