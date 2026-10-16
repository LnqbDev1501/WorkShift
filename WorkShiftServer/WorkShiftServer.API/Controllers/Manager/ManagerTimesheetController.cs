using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Route("api/manager/timesheets")]
[Authorize(Policy = "Manager")]
public sealed class ManagerTimesheetController : ControllerBase
{
    private readonly IManagerTimesheetService _timesheetService;
    private readonly ILogger<ManagerTimesheetController> _logger;

    public ManagerTimesheetController(
        IManagerTimesheetService timesheetService,
        ILogger<ManagerTimesheetController> logger)
    {
        _timesheetService = timesheetService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves weekly timesheet list with search and filters for the authenticated manager's branch.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>>> GetWeeklyTimesheets(
        [FromQuery] ManagerTimesheetListQuery query,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.GetWeeklyTimesheetsAsync(managerId, query, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weekly timesheets for manager {ManagerId}", managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve weekly timesheet list.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves weekly timesheet details for a specific week.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ManagerWeeklyTimesheetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerWeeklyTimesheetDetailResponse>> GetWeeklyTimesheetDetail(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.GetWeeklyTimesheetDetailAsync(managerId, id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Weekly timesheet details not found for week ID {id}." });
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
            _logger.LogError(ex, "Error retrieving weekly timesheet detail for week {ScheduleWeekId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve weekly timesheet details.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Exports weekly timesheet to Excel (.xlsx) format.
    /// </summary>
    [HttpGet("{id:int}/export-excel")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportWeeklyTimesheetExcel(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var (fileBytes, fileName) = await _timesheetService.ExportWeeklyTimesheetExcelAsync(managerId, id, cancellationToken);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting weekly timesheet to Excel for week {ScheduleWeekId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to export timesheet to Excel.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Generates weekly timesheets for a completed schedule week with an OPEN payroll period.
    /// </summary>
    [HttpPost("{id:int}/generate")]
    [ProducesResponseType(typeof(ManagerWeeklyTimesheetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerWeeklyTimesheetDetailResponse>> GenerateWeeklyTimesheet(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.GenerateWeeklyTimesheetAsync(managerId, id, cancellationToken);
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
            _logger.LogError(ex, "Error generating weekly timesheet for week {ScheduleWeekId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to generate weekly timesheet.",
                detail = ex.GetBaseException().Message
            });
        }
    }

    /// <summary>
    /// Reviews/locks weekly timesheet batch for a schedule week, finalizing work hours and notifying branch accountant.
    /// </summary>
    [HttpPost("{id:int}/lock")]
    [HttpPost("{id:int}/review")]
    [ProducesResponseType(typeof(ManagerWeeklyTimesheetDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerWeeklyTimesheetDetailResponse>> LockWeeklyTimesheet(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.LockWeeklyTimesheetAsync(managerId, id, cancellationToken);
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
            _logger.LogError(ex, "Error locking weekly timesheet for week {ScheduleWeekId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to lock weekly timesheet.",
                detail = ex.GetBaseException().Message
            });
        }
    }

    /// <summary>
    /// Retrieves paged list of timesheet adjustment requests for manager's branch with KPI counts.
    /// </summary>
    [HttpGet("adjustments")]
    [ProducesResponseType(typeof(ManagerTimesheetAdjustmentPagedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagerTimesheetAdjustmentPagedResponse>> GetTimesheetAdjustments(
        [FromQuery] ManagerTimesheetAdjustmentListQuery query,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.GetTimesheetAdjustmentsAsync(managerId, query, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timesheet adjustments for manager {ManagerId}", managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve timesheet adjustment requests.",
                detail = ex.GetBaseException().Message
            });
        }
    }

    /// <summary>
    /// Retrieves detailed information for a single timesheet adjustment request.
    /// </summary>
    [HttpGet("adjustments/{id:int}")]
    [ProducesResponseType(typeof(ManagerTimesheetAdjustmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerTimesheetAdjustmentDto>> GetAdjustmentDetail(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.GetAdjustmentDetailAsync(managerId, id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Adjustment request #{id} not found." });
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
            _logger.LogError(ex, "Error retrieving timesheet adjustment detail #{AdjustmentRequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to retrieve adjustment request details.",
                detail = ex.GetBaseException().Message
            });
        }
    }

    /// <summary>
    /// Approves a pending timesheet adjustment request and recalculates attendance hours.
    /// </summary>
    [HttpPost("adjustments/{id:int}/approve")]
    [ProducesResponseType(typeof(ManagerTimesheetAdjustmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerTimesheetAdjustmentDto>> ApproveAdjustment(
        int id,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        try
        {
            var result = await _timesheetService.ApproveAdjustmentRequestAsync(managerId, id, cancellationToken);
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
            _logger.LogError(ex, "Error approving timesheet adjustment #{AdjustmentRequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to approve adjustment request.",
                detail = ex.GetBaseException().Message
            });
        }
    }

    /// <summary>
    /// Rejects a pending timesheet adjustment request with a manager note.
    /// </summary>
    [HttpPost("adjustments/{id:int}/reject")]
    [ProducesResponseType(typeof(ManagerTimesheetAdjustmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerTimesheetAdjustmentDto>> RejectAdjustment(
        int id,
        [FromBody] RejectAdjustmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId == Guid.Empty)
        {
            return Unauthorized(new { message = "Could not identify authenticated manager identity from token." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _timesheetService.RejectAdjustmentRequestAsync(managerId, id, dto, cancellationToken);
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
            _logger.LogError(ex, "Error rejecting timesheet adjustment #{AdjustmentRequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to reject adjustment request.",
                detail = ex.GetBaseException().Message
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
