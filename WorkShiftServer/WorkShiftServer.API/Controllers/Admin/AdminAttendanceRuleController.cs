using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/attendance-rules")]
public class AdminAttendanceRuleController : ControllerBase
{
    private readonly IAttendanceRuleService _attendanceRuleService;
    private readonly ILogger<AdminAttendanceRuleController> _logger;

    public AdminAttendanceRuleController(
        IAttendanceRuleService attendanceRuleService,
        ILogger<AdminAttendanceRuleController> logger)
    {
        _attendanceRuleService = attendanceRuleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AttendanceRuleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceRuleListResponse>> GetAttendanceRules(
        [FromQuery] GetAttendanceRulesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceRuleService.GetAttendanceRulesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance rules");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Attendance rules could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceRuleResponse>> GetAttendanceRuleById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "id must be a positive integer." });
            }

            var rule = await _attendanceRuleService.GetByIdAsync(id, cancellationToken);
            return Ok(rule);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The attendance rule could not be retrieved. Please try again later." });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceRuleResponse>> CreateAttendanceRule(
        [FromBody] CreateAttendanceRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Could not identify authenticated admin identity from token." });
            }

            var createdRule = await _attendanceRuleService.CreateAsync(request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, createdRule);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating attendance rule");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The attendance rule could not be created. Please try again later." });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceRuleResponse>> UpdateAttendanceRule(
        int id,
        [FromBody] UpdateAttendanceRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedRule = await _attendanceRuleService.UpdateAsync(id, request, cancellationToken);
            return Ok(updatedRule);
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating attendance rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The attendance rule could not be updated. Please try again later." });
        }
    }

    [HttpPost("{id:int}/publish")]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendanceRuleResponse>> PublishAttendanceRule(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Could not identify authenticated admin identity from token." });
            }

            var publishedRule = await _attendanceRuleService.PublishAsync(id, userId, cancellationToken);
            return Ok(publishedRule);
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while publishing attendance rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The attendance rule could not be published. Please try again later." });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }
}
