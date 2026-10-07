using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/dashboard")]
public sealed class ManagerDashboardController(IManagerDashboardService service, ILogger<ManagerDashboardController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ManagerDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ManagerDashboardResponse>> Get(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var managerId) || managerId == Guid.Empty)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try { return Ok(await service.GetAsync(managerId, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve dashboard for manager {ManagerId}", managerId);
            return StatusCode(500, new { message = "Unable to load the dashboard. Please try again later." });
        }
    }
}
