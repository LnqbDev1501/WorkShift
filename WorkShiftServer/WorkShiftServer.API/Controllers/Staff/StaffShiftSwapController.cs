using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Authorize(Roles = "STAFF")]
[Route("api/staff/shift-swap-requests")]
[ProducesResponseType(typeof(ProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
public sealed class StaffShiftSwapController(IStaffShiftSwapService service, ILogger<StaffShiftSwapController> logger) : ControllerBase
{
    [HttpGet("candidates")]
    [ProducesResponseType(typeof(List<StaffShiftSwapCandidateResponse>), 200)]
    public Task<IActionResult> Candidates([FromQuery] int sourceAssignmentId, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetCandidatesAsync(actor, sourceAssignmentId, ct)));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StaffShiftSwapResponse>), 200)]
    public Task<IActionResult> List([FromQuery] ShiftSwapListQuery query, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetPagedAsync(actor, query, ct)));

    [HttpGet("{requestId:int}")]
    [ProducesResponseType(typeof(StaffShiftSwapResponse), 200)]
    public Task<IActionResult> Detail(int requestId, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.GetDetailAsync(actor, requestId, ct)));

    [HttpPost]
    [ProducesResponseType(typeof(StaffShiftSwapResponse), 200)]
    public Task<IActionResult> Submit([FromBody] SubmitShiftSwapRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.SubmitAsync(actor, request, ct)));

    [HttpPost("{requestId:int}/cancel")]
    [ProducesResponseType(typeof(StaffShiftSwapResponse), 200)]
    public Task<IActionResult> Cancel(int requestId, [FromBody] RespondShiftSwapRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.CancelAsync(actor, requestId, request, ct)));

    [HttpPost("{requestId:int}/accept")]
    [ProducesResponseType(typeof(StaffShiftSwapResponse), 200)]
    public Task<IActionResult> Accept(int requestId, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.AcceptAsync(actor, requestId, ct)));

    [HttpPost("{requestId:int}/reject")]
    [ProducesResponseType(typeof(StaffShiftSwapResponse), 200)]
    public Task<IActionResult> Reject(int requestId, [FromBody] RespondShiftSwapRequest request, CancellationToken ct) =>
        ExecuteAsync(async actor => Ok(await service.RejectAsync(actor, requestId, request, ct)));

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
            logger.LogError(ex, "Failed to process shift swap for staff {StaffId}", actor);
            return Problem(statusCode: 500, detail: "Unable to process the shift swap. Please try again later.");
        }
    }
}
