using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;
[ApiController, Authorize(Roles = "ADMIN,ACCOUNTANT,STAFF,MANAGER"), Route("api/payroll-system-notifications")]
public sealed class PayrollSystemNotificationsController(IPayrollSystemNotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor)) return Unauthorized();
        try { return Ok(await service.ListAsync(actor, page, pageSize, ct)); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Status = 400, Detail = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
