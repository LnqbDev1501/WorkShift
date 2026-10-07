using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController]
[Authorize(Roles = "ACCOUNTANT")]
[Route("api/accountant/dashboard")]
public sealed class AccountantDashboardController(IAccountantDashboardService service, ILogger<AccountantDashboardController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AccountantDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AccountantDashboardResponse>> Get(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var accountantId) || accountantId == Guid.Empty)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try { return Ok(await service.GetAsync(accountantId, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve dashboard for accountant {AccountantId}", accountantId);
            return StatusCode(500, new { message = "Unable to load the dashboard. Please try again later." });
        }
    }
}
