using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.Payslip;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController, Authorize(Roles = "ADMIN,ACCOUNTANT,STAFF"), Route("api/payslips")]
public sealed class PayslipController(IPayslipService service) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<object>> action)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor) || actor == Guid.Empty)
                throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
            return Ok(await action(actor));
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
    }

    [HttpGet]
    public Task<IActionResult> List([FromQuery] PayslipListRequest query, CancellationToken ct)
        => Execute(async actor => await service.ListAsync(actor, query, ct));
    [HttpGet("{id:int}")]
    public Task<IActionResult> Details(int id, CancellationToken ct)
        => Execute(async actor => await service.GetAsync(actor, id, ct));
    [HttpGet("{id:int}/versions")]
    public Task<IActionResult> Versions(int id, CancellationToken ct)
        => Execute(async actor => await service.VersionsAsync(actor, id, ct));
}
