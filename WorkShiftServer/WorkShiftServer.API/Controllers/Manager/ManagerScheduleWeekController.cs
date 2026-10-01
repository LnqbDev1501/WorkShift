using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Route("api/manager/schedule-weeks")]
[Authorize(Policy = "Manager")]
public sealed class ManagerScheduleWeekController(IScheduleWeekService scheduleWeekService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduleWeekSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetList([FromQuery] ScheduleWeekListQuery query) =>
        ExecuteAsync(async managerId => Ok(await scheduleWeekService.GetListAsync(managerId, query)));

    [HttpGet("{weekId:int}")]
    [ProducesResponseType(typeof(ScheduleWeekDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int weekId) =>
        ExecuteAsync(async managerId => Ok(await scheduleWeekService.GetByIdAsync(managerId, weekId)));

    [HttpPost]
    [ProducesResponseType(typeof(ScheduleWeekDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateScheduleWeekRequest request) =>
        ExecuteAsync(async managerId =>
        {
            var result = await scheduleWeekService.CreateAsync(managerId, request);
            return CreatedAtAction(nameof(GetById), new { weekId = result.ScheduleWeekId }, result);
        });

    [HttpPut("{weekId:int}")]
    [ProducesResponseType(typeof(ScheduleWeekDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(int weekId, [FromBody] UpdateScheduleWeekRequest request) =>
        ExecuteAsync(async managerId => Ok(await scheduleWeekService.UpdateAsync(managerId, weekId, request)));

    [HttpDelete("{weekId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Delete(int weekId) =>
        ExecuteAsync(async managerId =>
        {
            await scheduleWeekService.DeleteAsync(managerId, weekId);
            return NoContent();
        });

    [HttpPost("{weekId:int}/open-registration")]
    [ProducesResponseType(typeof(ScheduleWeekDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> OpenRegistration(int weekId) =>
        ExecuteAsync(async managerId =>
            Ok(await scheduleWeekService.OpenRegistrationAsync(managerId, weekId)));

    [HttpPost("{weekId:int}/close-registration")]
    [ProducesResponseType(typeof(ScheduleWeekDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CloseRegistration(int weekId) =>
        ExecuteAsync(async managerId =>
            Ok(await scheduleWeekService.CloseRegistrationAsync(managerId, weekId)));

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        if (!Guid.TryParse(subject, out var managerId))
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "The authenticated user ID is invalid.");

        try
        {
            return await action(managerId);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: exception.Message);
        }
    }
}
