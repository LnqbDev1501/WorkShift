using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController]
[Authorize(Roles = "ADMIN,ACCOUNTANT")]
[Route("api/payroll-periods")]
public sealed class PayrollPeriodReportController(IPayrollPeriodService service) : ControllerBase
{
    [HttpGet("{id:int}/report")]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var actor) || actor == Guid.Empty) return Unauthorized();
        try
        {
            var report = await service.ExportAsync(actor, id, ct);
            return File(report.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.FileName);
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { UnauthorizedAccessException => 403, KeyNotFoundException => 404, ArgumentException => 400, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
    }
}
