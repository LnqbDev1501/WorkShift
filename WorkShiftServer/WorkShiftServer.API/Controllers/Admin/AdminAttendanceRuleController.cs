using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/attendance-rules")]
[AttendanceRuleValidation]
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
    [ProducesResponseType(typeof(List<AttendanceRuleResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _attendanceRuleService.GetAllAsync(cancellationToken)));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "id must be a positive integer." });
            }

            return Ok(await _attendanceRuleService.GetByIdAsync(id, cancellationToken));
        });

    [HttpPost]
    [ProducesResponseType(typeof(AttendanceRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateAttendanceRuleRequest request, CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(Unauthorized(new { message = "Could not identify authenticated admin identity from token." }));
        }

        return ExecuteAsync(async () => StatusCode(StatusCodes.Status201Created,
            await _attendanceRuleService.CreateAsync(request, userId, cancellationToken)));
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (exception is ArgumentException or ValidationException)
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Attendance rule operation failed.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The operation could not be completed. Please refresh and try again." });
        }
    }
}

internal sealed class AttendanceRuleValidationAttribute : ActionFilterAttribute
{
    // Order = -2001 runs before the default ApiController ModelStateInvalidFilter (order -2000)
    public AttendanceRuleValidationAttribute() => Order = -2001;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("id", out var idObj) && idObj is int id && id <= 0)
        {
            context.Result = new BadRequestObjectResult(new { message = "id must be a positive integer." });
            return;
        }

        if (context.ModelState.IsValid)
            return;

        var error = context.ModelState
            .Where(entry => entry.Value!.Errors.Count > 0)
            .OrderByDescending(entry => entry.Key.StartsWith('$'))
            .FirstOrDefault();

        if (error.Key is null)
            return;

        var field = error.Key;
        var modelError = error.Value!.Errors[0];

        var cleanField = field
            .TrimStart('$', '.')
            .Replace("['", "")
            .Replace("']", "")
            .Replace("request.", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

        string message;
        if (cleanField.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            message = "id must be a positive integer.";
        }
        else if (field.StartsWith('$') || modelError.Exception is not null || modelError.ErrorMessage.Contains("invalid start of a value", StringComparison.OrdinalIgnoreCase) || modelError.ErrorMessage.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
        {
            message = cleanField switch
            {
                "maxattendancedistancemeters" => "maxAttendanceDistanceMeters must be a valid integer number greater than 0.",
                "effectivefrom" => "effectiveFrom must be a valid date in YYYY-MM-DD format (e.g. '2026-10-01').",
                "checkinbeforeminutes" => "checkInBeforeMinutes must be a valid integer number between 0 and 1440.",
                "checkingraceminutes" => "checkInGraceMinutes must be a valid integer number between 0 and 1440.",
                "checkoutgraceminutes" => "checkOutGraceMinutes must be a valid integer number between 0 and 1440.",
                "checkoutafterminutes" => "checkOutAfterMinutes must be a valid integer number between 0 and 1440.",
                "rulename" => "ruleName must be a valid text string.",
                "description" => "description must be a valid text string.",
                _ => "The request body contains invalid data. Please ensure all numeric and date fields contain valid values and not text."
            };
        }
        else if (string.IsNullOrEmpty(field) || field == "request")
        {
            message = "The request body is required. Send a valid JSON object containing attendance rule configurations.";
        }
        else
        {
            message = modelError.ErrorMessage;
        }

        context.Result = new BadRequestObjectResult(new { message });
    }
}
