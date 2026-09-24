using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Services.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController, Authorize(Policy = "Admin"), Route("api/holiday-pay-rules")]
public sealed class AdminHolidayPayRulesController(HolidayPayRuleService service) : ControllerBase
{
    private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id"), out var id)
        ? id : throw new ArgumentException("Unable to identify the authenticated administrator.");

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException || ex is InvalidOperationException && ex.Data.Contains("code"))
        {
            var code = ex is ArgumentException ? 400 : ex is KeyNotFoundException ? 404 : 409;
            var problem = new ProblemDetails { Status = code, Detail = ex.Message };
            problem.Extensions["code"] = ex.Data["code"] ?? "INVALID_RULE";
            return StatusCode(code, problem);
        }
    }
    [HttpGet] public Task<IActionResult> List(CancellationToken ct) => Run(async () => Ok(await service.ListAsync(ct)));
    [HttpPost("{id:int}/publish")] public Task<IActionResult> Publish(int id, CancellationToken ct) =>
        Run(async () => Ok(await service.PublishAsync(id, Actor, ct)));
    [HttpPost("{id:int}/cancel")] public Task<IActionResult> Cancel(int id, CancellationToken ct) =>
        Run(async () => Ok(await service.CancelAsync(id, Actor, ct)));
    [HttpPost] public Task<IActionResult> Create(HolidayPayRuleRequest request, CancellationToken ct) =>
        Run(async () => StatusCode(201, await service.SaveAsync(null, request, Actor, ct)));
    [HttpPut("{id:int}")] public Task<IActionResult> Update(int id, HolidayPayRuleRequest request, CancellationToken ct) =>
        Run(async () => Ok(await service.SaveAsync(id, request, Actor, ct)));
    [HttpDelete("{id:int}")] public Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Run(async () => { await service.DeleteAsync(id, Actor, ct); return NoContent(); });
}
