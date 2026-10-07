using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Request;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController]
[Authorize(Roles = "ACCOUNTANT")]
[Route("api/accountant/payroll-periods")]
public sealed class AccountantPayrollPeriodController(IPayrollPeriodService service) : ControllerBase
{
    private Guid ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated Accountant ID is invalid.");
        return id;
    }

    private async Task<IActionResult> ExecuteAsync(Func<Guid, Task<IActionResult>> action)
    {
        try { return await action(ActorId()); }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { UnauthorizedAccessException => 403, KeyNotFoundException => 404, ArgumentException => 400, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PayrollPeriodListRequest query, CancellationToken ct)
        => ExecuteAsync(async actor => Ok(await service.GetAllAsync(actor, query, ct)));

    [HttpGet("available-schedule-weeks")]
    public Task<IActionResult> AvailableWeeks([FromQuery] PayrollPeriodListRequest query, CancellationToken ct)
        => ExecuteAsync(async actor => Ok(await service.GetAvailableWeeksAsync(actor, query, ct)));

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => ExecuteAsync(async actor => Ok(await service.GetByIdAsync(actor, id, ct)));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SavePayrollPeriodRequest request, CancellationToken ct)
        => ExecuteAsync(async actor =>
        {
            var result = await service.CreateAsync(actor, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.PayrollPeriodId }, result);
        });

    [HttpPatch("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePayrollPeriodRequest request, CancellationToken ct)
        => ExecuteAsync(async actor => Ok(await service.UpdateAsync(actor, id, request, ct)));

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => ExecuteAsync(async actor => { await service.DeleteAsync(actor, id, ct); return NoContent(); });

    [HttpPost("{id:int}/open")]
    public Task<IActionResult> Open(int id, CancellationToken ct)
        => ExecuteAsync(async actor => Ok(await service.OpenAsync(actor, id, ct)));
}
