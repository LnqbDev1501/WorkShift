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

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
