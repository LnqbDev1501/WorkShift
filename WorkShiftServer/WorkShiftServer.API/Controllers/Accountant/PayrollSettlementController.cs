using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollSettlement;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController, Authorize(Roles = "ACCOUNTANT"), Route("api")]
public sealed class PayrollSettlementController(IPayrollSettlementService service) : ControllerBase
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

    [HttpGet("payroll-periods/{id:int}/settlement-summary")]
    public Task<IActionResult> Summary(int id, CancellationToken ct)
        => Execute(async actor => await service.SummaryAsync(actor, id, ct));

    [HttpPost("payroll-periods/{id:int}/confirm-settlement")]
    public Task<IActionResult> Confirm(int id, ConfirmPayrollSettlementRequest request, CancellationToken ct)
        => Execute(async actor => await service.ConfirmAsync(actor, id, request, ct));

    [HttpGet("carry-forward-deductions")]
    public Task<IActionResult> List([FromQuery] CarryForwardListRequest query, CancellationToken ct)
        => Execute(async actor => await service.ListCarryAsync(actor, query, ct));

    [HttpGet("carry-forward-deductions/{id:int}")]
    public Task<IActionResult> Details(int id, CancellationToken ct)
        => Execute(async actor => await service.GetCarryAsync(actor, id, ct));

    [HttpGet("carry-forward-deductions/{id:int}/applications")]
    public Task<IActionResult> Applications(int id, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Execute(async actor => await service.ApplicationsAsync(actor, id, page, pageSize, ct));
}
