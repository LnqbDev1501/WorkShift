using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Attendance;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/attendance")]
public class ManagerAttendanceController : ControllerBase
{
    private readonly IManagerAttendanceService _attendanceService;
    private readonly ILogger<ManagerAttendanceController> _logger;

    public ManagerAttendanceController(
        IManagerAttendanceService attendanceService,
        ILogger<ManagerAttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ManagerDailyAttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDailyAttendance(
        [FromQuery] int? scheduleWeekId,
        [FromQuery] DateOnly? date,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? shiftId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        if (!Guid.TryParse(subject, out var managerId) || managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        var query = new ManagerDailyAttendanceQuery
        {
            ScheduleWeekId = scheduleWeekId,
            Date = date,
            Search = search,
            Status = status ?? "ALL",
            ShiftId = shiftId,
            PageIndex = Math.Max(1, pageIndex),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };

        try
        {
            var result = await _attendanceService.GetDailyAttendanceAsync(managerId, query, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load daily attendance for manager {ManagerId}", managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while loading daily attendance." });
        }
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(ManagerAttendanceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAttendanceDetail(
        [FromRoute] int assignmentId,
        CancellationToken cancellationToken = default)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        if (!Guid.TryParse(subject, out var managerId) || managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var detail = await _attendanceService.GetAttendanceDetailAsync(managerId, assignmentId, cancellationToken);
            if (detail == null)
            {
                return NotFound(new { message = "Attendance record or shift assignment not found." });
            }

            return Ok(detail);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance detail for assignment {AssignmentId} and manager {ManagerId}", assignmentId, managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while loading attendance details." });
        }
    }
}
