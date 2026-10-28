using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftServer.API.Services;

namespace WorkShiftServer.API.Controllers.Manager;

/// <summary>
/// Staff account management for branch managers.
/// </summary>
[Route("api/manager/staff")]
[ApiController]
[Authorize(Roles = "MANAGER")]
public sealed class ManagerStaffController : ControllerBase
{
    private const string WriteRoles = "MANAGER";
    private readonly IManagerStaffService _staffService;
    private readonly IAuthService _authService;
    private readonly StaffSetupTokenService _tokenService;
    private readonly IStaffSetupEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManagerStaffController> _logger;

    public ManagerStaffController(
        IManagerStaffService staffService,
        IAuthService authService,
        StaffSetupTokenService tokenService,
        IStaffSetupEmailSender emailSender,
        IConfiguration configuration,
        ILogger<ManagerStaffController> logger)
    {
        _staffService = staffService;
        _authService = authService;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of staff accounts in the manager's branch.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StaffAccountPagedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStaffList([FromQuery] ManagerStaffListQuery query)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            return Ok(await _staffService.GetStaffListAsync(query, actor.Value));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Gets detailed information for a specific staff member.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StaffAccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffDetail(Guid id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var staff = await _staffService.GetStaffDetailAsync(id, actor.Value);
            if (staff == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Staff account not found."
                });
            }

            return Ok(staff);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Creates a new staff account in the manager's branch.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StaffAccountDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStaff([FromForm] CreateStaffAccountRequest request)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var result = await _staffService.CreateStaffAsync(request, actor.Value);

            // Send setup email with 3-minute token
            try
            {
                var token = _tokenService.CreateToken(result.UserId);
                var setupBaseUrl = _configuration["AdminUi:StaffSetupUrl"]
                    ?? throw new InvalidOperationException("Missing AdminUi:StaffSetupUrl configuration.");
                var setupLink = $"{setupBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";

                await _emailSender.SendAsync(result.Email, setupLink);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send staff setup email for user {UserId}", result.UserId);
                // Don't fail the request if email fails - staff is created
            }

            return CreatedAtAction(nameof(GetStaffDetail), new { id = result.UserId }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while creating staff account.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Bad Gateway",
                Detail = $"Authentication service rejected the request: {exception.Message}"
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating staff account.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Staff account could not be created. Please try again later."
            });
        }
    }

/// <summary>
    /// Updates a staff account's personal information.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = WriteRoles)]
    [ProducesResponseType(typeof(StaffAccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromForm] UpdateStaffAccountRequest request)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var result = await _staffService.UpdateStaffAsync(id, request, actor.Value);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Staff account not found."
                });
            }
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Deactivates a staff account (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateStaff(Guid id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var deleted = await _staffService.DeactivateStaffAsync(id, actor.Value);
            if (!deleted)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Staff account not found."
                });
            }

            return Ok(new { message = "Staff account deactivated successfully." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Restores a deactivated staff account.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreStaff(Guid id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var restored = await _staffService.RestoreStaffAsync(id, actor.Value, null);
            if (!restored)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Staff account not found or not in a restorable state."
                });
            }

            return Ok(new { message = "Staff account restored successfully." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
}
        }

    /// <summary>
    /// Resends the staff account setup email (3-minute token).
    /// Only works if the staff has not yet set a password.
    /// </summary>
    [HttpPost("{id:guid}/resend-setup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendStaffSetup(Guid id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            // First check if user exists and hasn't set password
            var canResend = await _staffService.ResendStaffSetupLinkAsync(id, actor.Value);
            if (!canResend)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Cannot Resend",
                    Detail = "Staff account not found, not in your branch, or password has already been set."
                });
            }

            // Get staff details for email
            var staff = await _staffService.GetStaffDetailAsync(id, actor.Value);
            if (staff == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Staff account not found."
                });
            }

            // Generate new token and send email
            var token = _tokenService.CreateToken(staff.UserId);
            var setupBaseUrl = _configuration["AdminUi:StaffSetupUrl"]
                ?? throw new InvalidOperationException("Missing AdminUi:StaffSetupUrl configuration.");
            var setupLink = $"{setupBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";

            await _emailSender.SendAsync(staff.Email, setupLink);

            return Ok(new { message = "Setup email resent successfully." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Email service error while resending staff setup link.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Bad Gateway",
                Detail = $"Email service error: {exception.Message}"
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while resending staff setup link.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Setup email could not be sent. Please try again later."
            });
        }
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }
}
