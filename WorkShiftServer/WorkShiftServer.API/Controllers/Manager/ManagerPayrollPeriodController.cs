using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/payroll-periods")]
public sealed class ManagerPayrollPeriodController(IEmployeeAdjustmentService service, ILogger<ManagerPayrollPeriodController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PayrollPeriodOptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var id) || id == Guid.Empty) return Unauthorized(new { message = "Tài khoản không hợp lệ." });
        try { return Ok(await service.PeriodsAsync(id, ct)); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Manager payroll period list failed.");
            return StatusCode(500, new ProblemDetails { Status = 500, Detail = "Không thể tải danh sách kỳ lương.", Instance = Request.Path });
        }
    }
}
