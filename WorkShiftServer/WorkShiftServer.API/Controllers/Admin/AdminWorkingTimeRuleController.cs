using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/working-time-rules")]
public class AdminWorkingTimeRuleController : ControllerBase
{
    private readonly IWorkingTimeRuleService _workingTimeRuleService;
    private readonly ILogger<AdminWorkingTimeRuleController> _logger;

    public AdminWorkingTimeRuleController(IWorkingTimeRuleService workingTimeRuleService, ILogger<AdminWorkingTimeRuleController> logger)
    {
        _workingTimeRuleService = workingTimeRuleService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _workingTimeRuleService.GetAllAsync(cancellationToken)));

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _workingTimeRuleService.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateWorkingTimeRuleRequest request, CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty)
            return Task.FromResult<IActionResult>(Unauthorized());
        return ExecuteAsync(async () => StatusCode(StatusCodes.Status201Created,
            await _workingTimeRuleService.CreateAsync(request, userId, cancellationToken)));
    }

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] CreateWorkingTimeRuleRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _workingTimeRuleService.UpdateAsync(id, request, cancellationToken)));

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        await _workingTimeRuleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    });

    [HttpPost("{id:int}/publish")]
    public Task<IActionResult> Publish(int id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _workingTimeRuleService.PublishAsync(id, cancellationToken)));

    [HttpPost("{id:int}/cancel")]
    public Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await _workingTimeRuleService.CancelAsync(id, cancellationToken)));

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Exception exception) when (exception is ArgumentException or ValidationException)
        { return BadRequest(new { message = exception.Message }); }
        catch (KeyNotFoundException exception)
        { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception)
        { return Conflict(new { message = exception.Message }); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Working time operation failed.");
            return StatusCode(500, new { message = "The operation could not be completed. Please refresh and try again." });
        }
    }
}
