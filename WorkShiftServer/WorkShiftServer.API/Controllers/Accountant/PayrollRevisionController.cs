using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;
namespace WorkShiftServer.API.Controllers.Accountant;
[ApiController, Authorize(Roles = "ADMIN,ACCOUNTANT"), Route("api/payrolls")]
public sealed class PayrollRevisionController(IPayrollRevisionService service) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<object>> action)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor) || actor == Guid.Empty)
                throw new UnauthorizedAccessException("Invalid user account.");
            return Ok(await action(actor));
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
    }
    [HttpPost("{id:int}/recalculate"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> Recalculate(int id, PayrollCorrectionRequest request, CancellationToken ct)
        => Execute(async actor => await service.RecalculateAsync(actor, id, request, ct));
    [HttpGet("{id:int}/versions")]
    public Task<IActionResult> Versions(int id, CancellationToken ct)
        => Execute(async actor => await service.VersionsAsync(actor, id, ct));
    [NonAction]
    public Task<IActionResult> Approve(int id, CancellationToken ct)
        => Execute(async actor => await service.ApproveCorrectionAsync(actor, id, ct));
}
