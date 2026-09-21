using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/working-time-rules")]
public class AdminWorkingTimeRuleController : ControllerBase
{
    private readonly IWorkingTimeRuleService _workingTimeRuleService;
    private readonly ILogger<AdminWorkingTimeRuleController> _logger;

    public AdminWorkingTimeRuleController(
        IWorkingTimeRuleService workingTimeRuleService,
        ILogger<AdminWorkingTimeRuleController> logger)
    {
        _workingTimeRuleService = workingTimeRuleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WorkingTimeRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<WorkingTimeRuleResponse>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _workingTimeRuleService.GetAllAsync(cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Unexpected error while retrieving working time rules.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Working time versions could not be retrieved. Please try again later." });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkingTimeRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkingTimeRuleResponse>> Create(
        [FromBody] CreateWorkingTimeRuleRequest request, CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty) return Unauthorized();

        try
        {
            var result = await _workingTimeRuleService.CreateAsync(request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (Exception exception) when (exception is ArgumentException or ValidationException)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Unexpected error while creating a working time version.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The version could not be created. Please try again later." });
        }
    }
}
