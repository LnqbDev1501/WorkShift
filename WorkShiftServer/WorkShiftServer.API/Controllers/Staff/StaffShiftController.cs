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
[Route("api/staff/shifts")]
[ProducesResponseType(typeof(ProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
public sealed class StaffShiftController(IStaffShiftService service, ILogger<StaffShiftController> logger) : ControllerBase
{
    [HttpGet("access")]
    [ProducesResponseType(typeof(StaffShiftAccessResponse), 200)]
    public Task<IActionResult> Access(CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.GetAccessAsync(staff, ct)));

    [HttpGet("available")]
    [ProducesResponseType(typeof(PagedResponse<StaffShiftResponse>), 200)]
    public Task<IActionResult> Available([FromQuery] StaffShiftListQuery query, CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.GetAvailableAsync(staff, query, ct)));

    [HttpPost("{shiftId:int}/register")]
    [ProducesResponseType(typeof(StaffShiftRegistrationResponse), 200)]
    public Task<IActionResult> Register(int shiftId, CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.RegisterAsync(staff, shiftId, ct)));

    [HttpPost("{shiftId:int}/cancel-registration")]
    [ProducesResponseType(typeof(StaffShiftRegistrationResponse), 200)]
    public Task<IActionResult> Cancel(int shiftId, [FromBody] CancelShiftRegistrationRequest request, CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.CancelAsync(staff, shiftId, request, ct)));

    [HttpGet("my-schedule")]
    [ProducesResponseType(typeof(PagedResponse<StaffShiftResponse>), 200)]
    public Task<IActionResult> MySchedule([FromQuery] StaffShiftListQuery query, CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.GetMyScheduleAsync(staff, query, ct)));

    [HttpGet("{shiftId:int}")]
    [ProducesResponseType(typeof(StaffShiftResponse), 200)]
    public Task<IActionResult> Detail(int shiftId, CancellationToken ct) =>
        ExecuteAsync(async staff => Ok(await service.GetDetailAsync(staff, shiftId, ct)));

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var staff) || staff == Guid.Empty)
            return Problem(statusCode: 401, detail: "Invalid authenticated user ID.");
        try { return await action(staff); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        catch (UnauthorizedAccessException ex) { return Problem(statusCode: 403, detail: ex.Message); }
        catch (KeyNotFoundException ex) { return Problem(statusCode: 404, detail: ex.Message); }
        catch (InvalidOperationException ex) { return Problem(statusCode: 409, detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process shift request for staff {StaffId}", staff);
            return Problem(statusCode: 500, detail: "Unable to process the shift request. Please try again later.");
        }
    }
}
