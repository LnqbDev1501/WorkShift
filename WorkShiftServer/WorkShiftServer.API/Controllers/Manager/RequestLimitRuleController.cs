using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/request-limit-rules")]
public class RequestLimitRuleController(IRequestLimitRuleService service, ILogger<RequestLimitRuleController> logger) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetPaged([FromQuery] RequestLimitRuleListQuery query, CancellationToken ct) =>
        ExecuteAsync(async manager => Ok(await service.GetPagedAsync(manager, query, ct)), ct);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct) =>
        ExecuteAsync(async manager => Ok(await service.GetAsync(manager, id, ct)), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRequestLimitRuleRequest request, CancellationToken ct) =>
        ExecuteAsync(async manager =>
        {
            var result = await service.CreateAsync(manager, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.RequestLimitRuleId }, result);
        }, ct);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRequestLimitRuleRequest request, CancellationToken ct) =>
        ExecuteAsync(async manager => Ok(await service.UpdateAsync(manager, id, request, ct)), ct);

    [HttpPost("{id:int}/publish")]
    public Task<IActionResult> Publish(int id, CancellationToken ct) =>
        ExecuteAsync(async manager => Ok(await service.PublishAsync(manager, id, ct)), ct);

    [HttpPost("{id:int}/replace")]
    public Task<IActionResult> Replace(int id, [FromBody] ReplaceRequestLimitRuleRequest request, CancellationToken ct) =>
        ExecuteAsync(async manager =>
        {
            var result = await service.ReplaceAsync(manager, id, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.RequestLimitRuleId }, result);
        }, ct);

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action, CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var manager)) return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try { return await action(manager); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request limit rule operation failed for manager {ManagerId}", manager);
            return StatusCode(500, new { message = "The request limit rule operation failed. Please try again later." });
        }
    }
}
