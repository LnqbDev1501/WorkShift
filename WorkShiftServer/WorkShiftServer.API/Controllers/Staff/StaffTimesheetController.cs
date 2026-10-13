using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Staff.Timesheet;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Route("api/staff/timesheets")]
[Authorize]
public sealed class StaffTimesheetController : ControllerBase
{
    private readonly IStaffTimesheetService _timesheetService;
    private readonly ILogger<StaffTimesheetController> _logger;

    public StaffTimesheetController(
        IStaffTimesheetService timesheetService,
        ILogger<StaffTimesheetController> logger)
    {
        _timesheetService = timesheetService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the weekly timesheet summary and detailed shift attendance breakdown for the authenticated staff member.
    /// </summary>
    /// <param name="scheduleWeekId">Optional schedule week ID. If omitted, the latest generated week is returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("my-timesheet")]
    [ProducesResponseType(typeof(StaffWeeklyTimesheetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffWeeklyTimesheetResponse>> GetMyWeeklyTimesheet(
        [FromQuery] int? scheduleWeekId,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        try
        {
            var result = await _timesheetService.GetMyWeeklyTimesheetAsync(staffId, scheduleWeekId, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = "No weekly timesheet or shift schedule records found for your branch." });
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weekly timesheet for staff {StaffId}, week {ScheduleWeekId}", staffId, scheduleWeekId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve weekly timesheet.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Confirms the weekly timesheet for the authenticated staff member.
    /// </summary>
    /// <param name="request">Confirmation request containing scheduleWeekId and optional note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(StaffWeeklyTimesheetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffWeeklyTimesheetResponse>> ConfirmTimesheet(
        [FromBody] StaffTimesheetConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated staff identity from token." });
        }

        if (request == null || request.ScheduleWeekId <= 0)
        {
            return BadRequest(new { message = "Valid scheduleWeekId is required to confirm timesheet." });
        }

        try
        {
            var result = await _timesheetService.ConfirmTimesheetAsync(staffId, request.ScheduleWeekId, request.Note, cancellationToken);
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
            _logger.LogError(ex, "Error confirming weekly timesheet for staff {StaffId}, week {ScheduleWeekId}", staffId, request.ScheduleWeekId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to confirm weekly timesheet.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
