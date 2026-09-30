using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Route("api/manager/shift-cancellation-requests")]
[Authorize(Roles = "MANAGER")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class ManagerShiftCancellationRequestController(
    IShiftCancellationRequestService service,
    ILogger<ManagerShiftCancellationRequestController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ShiftCancellationRequestResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetList([FromQuery] ShiftCancellationRequestListQuery query, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetListAsync(actor, query, ct)));

    [HttpGet("{requestId:int}")]
    [ProducesResponseType(typeof(ShiftCancellationRequestResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(int requestId, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetByIdAsync(actor, requestId, ct)));

    [HttpPost("{requestId:int}/approve")]
    [ProducesResponseType(typeof(ShiftCancellationRequestResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Approve(int requestId, [FromBody] ReviewShiftCancellationRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.ApproveAsync(actor, requestId, request, ct)));

    [HttpPost("{requestId:int}/reject")]
    [ProducesResponseType(typeof(ShiftCancellationRequestResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Reject(int requestId, [FromBody] ReviewShiftCancellationRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.RejectAsync(actor, requestId, request, ct)));

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
            logger.LogError(ex, "Failed to process shift cancellation request for {ActorId}", actor);
            return Problem(statusCode: 500, detail: "Unable to process the request. Please try again later.");
        }
    }
}
