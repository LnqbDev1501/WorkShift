using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/attendance-penalty-rules")]
public class AdminAttendancePenaltyRuleController : ControllerBase
{
    private readonly IAttendancePenaltyRuleService _attendancePenaltyRuleService;
    private readonly ILogger<AdminAttendancePenaltyRuleController> _logger;

    public AdminAttendancePenaltyRuleController(
        IAttendancePenaltyRuleService attendancePenaltyRuleService,
        ILogger<AdminAttendancePenaltyRuleController> logger)
    {
        _attendancePenaltyRuleService = attendancePenaltyRuleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AttendancePenaltyRuleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleListResponse>> GetAttendancePenaltyRules(
        [FromQuery] GetAttendancePenaltyRulesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendancePenaltyRuleService.GetAttendancePenaltyRulesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance penalty rules");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Attendance penalty rules could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AttendancePenaltyRuleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleDetailResponse>> GetAttendancePenaltyRuleById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "id must be a positive integer." });
            }

            var rule = await _attendancePenaltyRuleService.GetByIdAsync(id, cancellationToken);
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
            _logger.LogError(exception, "Unexpected error while retrieving attendance penalty rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The attendance penalty rule could not be retrieved. Please try again later." });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AttendancePenaltyRuleDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleDetailResponse>> CreateAttendancePenaltyRule(
        [FromBody] CreateAttendancePenaltyRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Could not identify authenticated admin identity from token." });
            }

            var createdRule = await _attendancePenaltyRuleService.CreateAsync(request, userId, cancellationToken);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
        {
            _logger.LogError(exception, "Database error while creating attendance penalty rule");
            var innerMessage = exception.InnerException?.Message ?? exception.Message;
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Database error: {innerMessage}" });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating attendance penalty rule");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AttendancePenaltyRuleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleDetailResponse>> UpdateAttendancePenaltyRule(
        int id,
        [FromBody] UpdateAttendancePenaltyRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedRule = await _attendancePenaltyRuleService.UpdateAsync(id, request, cancellationToken);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
        {
            _logger.LogError(exception, "Database error while updating attendance penalty rule {Id}", id);
            var innerMessage = exception.InnerException?.Message ?? exception.Message;
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Database error: {innerMessage}" });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating attendance penalty rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = exception.Message });
        }
    }

    [HttpPost("{id:int}/publish")]
    [ProducesResponseType(typeof(AttendancePenaltyRuleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleDetailResponse>> PublishAttendancePenaltyRule(
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

            var publishedRule = await _attendancePenaltyRuleService.PublishAsync(id, userId, cancellationToken);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
        {
            _logger.LogError(exception, "Database error while publishing attendance penalty rule {Id}", id);
            var innerMessage = exception.InnerException?.Message ?? exception.Message;
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Database error: {innerMessage}" });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while publishing attendance penalty rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = exception.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(AttendancePenaltyRuleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleDetailResponse>> CancelAttendancePenaltyRule(
        int id,
        [FromBody] CancelAttendancePenaltyRuleRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Could not identify authenticated admin identity from token." });
            }

            var cancelledRule = await _attendancePenaltyRuleService.CancelAsync(id, request, userId, cancellationToken);
            return Ok(cancelledRule);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
        {
            _logger.LogError(exception, "Database error while cancelling attendance penalty rule {Id}", id);
            var innerMessage = exception.InnerException?.Message ?? exception.Message;
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Database error: {innerMessage}" });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while cancelling attendance penalty rule {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = exception.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value;

        return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
    }
}
