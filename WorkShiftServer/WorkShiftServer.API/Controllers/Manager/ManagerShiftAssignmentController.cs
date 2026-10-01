using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager")]
[ProducesResponseType(typeof(ProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
public sealed class ManagerShiftAssignmentController(
    IShiftAssignmentService service, ILogger<ManagerShiftAssignmentController> logger) : ControllerBase
{
    [HttpPost("shifts/{shiftId:int}/assignments")]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), 200)]
    public Task<IActionResult> Assign(int shiftId, [FromBody] AssignShiftRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.AssignAsync(actor, shiftId, request, ct)));

    [HttpPost("assignments/{assignmentId:int}/cancel")]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), 200)]
    public Task<IActionResult> Cancel(int assignmentId, [FromBody] CancelShiftAssignmentRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.CancelAsync(actor, assignmentId, request, ct)));

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var actor) || actor == Guid.Empty)
            return Problem(statusCode: 401, detail: "Invalid authenticated user ID.");
        try { return await action(actor); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        catch (UnauthorizedAccessException ex) { return Problem(statusCode: 403, detail: ex.Message); }
        catch (KeyNotFoundException ex) { return Problem(statusCode: 404, detail: ex.Message); }
        catch (InvalidOperationException ex) { return Problem(statusCode: 409, detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process shift assignment for manager {ActorId}", actor);
            return Problem(statusCode: 500, detail: "Unable to process the assignment. Please try again later.");
        }
    }
}
