using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Route("api/staff/attendance")]
[Authorize]
public sealed class StaffAttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<StaffAttendanceController> _logger;

    public StaffAttendanceController(
        IAttendanceService attendanceService,
        ILogger<StaffAttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves today's eligible shift assignment and check-in eligibility details for the authenticated staff member.
    /// </summary>
    [HttpGet("eligible-shift")]
    [ProducesResponseType(typeof(EligibleShiftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EligibleShiftResponse>> GetEligibleShift(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _attendanceService.GetEligibleShiftAsync(userId, cancellationToken);
            if (result == null)
            {
                return Ok((EligibleShiftResponse?)null);
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible shift for staff {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve eligible shift.",
                error = ex.Message,
                detail = ex.InnerException?.Message
            });
        }
    }


    /// <summary>
    /// Retrieves chronological attendance history for the authenticated staff member with status (COMPLETED, IN_PROGRESS, INCOMPLETE, ABSENT).
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<StaffAttendanceHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<StaffAttendanceHistoryResponse>>> GetAttendanceHistory(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var results = await _attendanceService.GetStaffAttendanceHistoryAsync(userId, fromDate, toDate, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance history for staff {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve attendance history.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Creates an active liveness challenge session for check-in (TTL: 90 seconds).
    /// </summary>
    [HttpPost("check-in/session")]
    [ProducesResponseType(typeof(CheckInSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckInSessionResponse>> CreateCheckInSession([FromQuery] int? assignmentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _attendanceService.CreateCheckInSessionAsync(userId, assignmentId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating check-in face session for staff {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to create check-in verification session.",
                error = ex.Message,
                detail = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Performs shift attendance check-in with GPS Geofencing, biometric face verification, and rule snapshotting.
    /// </summary>
    [HttpPost("check-in")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckInResponse>> CheckIn(
        [FromForm] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var response = await _attendanceService.CheckInAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking in staff {UserId} for assignment {AssignmentId}", userId, request.AssignmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to process check-in. Please try again.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Creates an active liveness challenge session for check-out (TTL: 90 seconds).
    /// </summary>
    [HttpPost("check-out/session")]
    [ProducesResponseType(typeof(CheckOutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckOutSessionResponse>> CreateCheckOutSession(
        [FromQuery] int assignmentId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _attendanceService.CreateCheckOutSessionAsync(userId, assignmentId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating check-out face session for staff {UserId}, assignment {AssignmentId}", userId, assignmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to create check-out verification session.",
                error = ex.Message,
                detail = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Performs shift attendance check-out with GPS Geofencing, biometric face verification, early leave calculation, and shift completion.
    /// </summary>
    [HttpPost("check-out")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CheckOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckOutResponse>> CheckOut(
        [FromForm] CheckOutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var response = await _attendanceService.CheckOutAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking out staff {UserId} for assignment {AssignmentId}", userId, request.AssignmentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to process check-out. Please try again.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
