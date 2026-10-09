using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController]
[Authorize(Roles = "ADMIN,ACCOUNTANT")]
[Route("api")]
public sealed class PayrollCalculationController(IPayrollCalculationService service) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<IActionResult>> action)
    {
        try
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty) throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
            return await action(id);
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { UnauthorizedAccessException => 403, KeyNotFoundException => 404, ArgumentException => 400, _ => 409 };
            var problem = new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path };
            if (ex is PayrollCalculationBlockedException blocked) problem.Extensions["readiness"] = blocked.Readiness;
            return StatusCode(status, problem);
        }
    }

    [Authorize(Roles = "ACCOUNTANT")]
    [HttpGet("payroll-periods/{id:int}/calculation-readiness")]
    public Task<IActionResult> Readiness(int id, CancellationToken ct)
        => Execute(async actor => Ok(await service.GetReadinessAsync(actor, id, ct)));

    [Authorize(Roles = "ACCOUNTANT")]
    [HttpPost("payroll-periods/{id:int}/employees/{userId:guid}/calculate")]
    public Task<IActionResult> CalculateEmployee(int id, Guid userId, CancellationToken ct)
        => Execute(async actor => Ok(await service.CalculateEmployeeAsync(actor, id, userId, ct)));

    [Authorize(Roles = "ACCOUNTANT")]
    [HttpPost("payroll-periods/{id:int}/calculate")]
    public Task<IActionResult> CalculatePeriod(int id, CancellationToken ct)
        => Execute(async actor => Ok(await service.CalculatePeriodAsync(actor, id, ct)));

    [HttpGet("payroll-periods/{id:int}/payrolls")]
    public Task<IActionResult> Payrolls(int id, [FromQuery] PayrollCalculationListRequest query, CancellationToken ct)
        => Execute(async actor => Ok(await service.GetPayrollsAsync(actor, id, query, ct)));

    [HttpGet("payrolls/{id:int}")]
    public Task<IActionResult> Payroll(int id, CancellationToken ct)
        => Execute(async actor => Ok(await service.GetPayrollAsync(actor, id, ct)));

    [HttpGet("payrolls/{id:int}/details")]
    public Task<IActionResult> Details(int id, CancellationToken ct)
        => Execute(async actor => Ok((await service.GetPayrollAsync(actor, id, ct)).Details));
}
