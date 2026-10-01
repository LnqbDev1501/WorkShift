using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Route("api/manager/shifts")]
[Authorize(Roles = "MANAGER")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class ManagerShiftController(IShiftService service, ILogger<ManagerShiftController> logger) : ControllerBase
{
    [HttpPost("/api/manager/schedule-weeks/{weekId:int}/shifts")]
    [ProducesResponseType(typeof(ShiftDetailResponse), StatusCodes.Status201Created)]
    public Task<IActionResult> Create(int weekId, [FromBody] CreateShiftRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor =>
        {
            var result = await service.CreateAsync(actor, weekId, request, ct);
            return CreatedAtAction(nameof(GetById), new { shiftId = result.ShiftId }, result);
        });

    [HttpGet("{shiftId:int}")]
    [ProducesResponseType(typeof(ShiftDetailResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(int shiftId, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetByIdAsync(actor, shiftId, ct)));

    [HttpPut("{shiftId:int}")]
    [ProducesResponseType(typeof(ShiftDetailResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int shiftId, [FromBody] UpdateShiftRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.UpdateAsync(actor, shiftId, request, ct)));

    [HttpDelete("{shiftId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Delete(int shiftId, CancellationToken ct) =>
        ExecuteAsync(async actor =>
        {
            await service.DeleteAsync(actor, shiftId, ct);
            return NoContent();
        });

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var actor) || actor == Guid.Empty)
            return Problem(statusCode: 401, detail: "The authenticated user ID is invalid.");
        try { return await action(actor); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        catch (UnauthorizedAccessException ex) { return Problem(statusCode: 403, detail: ex.Message); }
        catch (KeyNotFoundException ex) { return Problem(statusCode: 404, detail: ex.Message); }
        catch (InvalidOperationException ex) { return Problem(statusCode: 409, detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process manager shift request for {ActorId}", actor);
            return Problem(statusCode: 500, detail: "Unable to process the shift request. Please try again later.");
        }
    }
}
