using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Route("api/staff/face-registration")]
[Authorize]
public sealed class StaffFaceRegistrationController : ControllerBase
{
    private readonly IFaceRegistrationService _faceRegistrationService;
    private readonly ILogger<StaffFaceRegistrationController> _logger;

    public StaffFaceRegistrationController(
        IFaceRegistrationService faceRegistrationService,
        ILogger<StaffFaceRegistrationController> logger)
    {
        _faceRegistrationService = faceRegistrationService;
        _logger = logger;
    }

    /// <summary>
    /// Checks the current authenticated staff member's face registration status.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(FaceProfileStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FaceProfileStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        var result = await _faceRegistrationService.GetStatusAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new face verification session with random active liveness challenges (TTL: 90 seconds).
    /// </summary>
    [HttpPost("session")]
    [ProducesResponseType(typeof(FaceRegistrationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FaceRegistrationSessionResponse>> CreateSession(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _faceRegistrationService.CreateSessionAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error creating face registration session for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to create face registration session. Please try again." });
        }
    }

    /// <summary>
    /// Cancels an active face verification session.
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSession(
        [FromBody] CancelFaceRegistrationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            await _faceRegistrationService.CancelSessionAsync(userId, request.SessionId, cancellationToken);
            return Ok(new { success = true, message = "Face registration session cancelled successfully." });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error cancelling session {SessionId} for user {UserId}", request.SessionId, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to cancel face registration session." });
        }
    }

    /// <summary>
    /// Verifies biometric face data from mobile capture and registers the active face profile.
    /// </summary>
    [HttpPost("verify")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FaceRegistrationVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FaceRegistrationVerifyResponse>> VerifyAndRegister(
        [FromForm] VerifyFaceRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _faceRegistrationService.VerifyAndRegisterAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error verifying face registration for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { 
                message = "Failed to verify face registration. Please try again.",
                detail = exception.InnerException?.Message ?? exception.Message
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
