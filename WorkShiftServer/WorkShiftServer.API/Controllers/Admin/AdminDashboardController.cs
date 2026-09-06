using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[Route("api/admin/dashboard")]
[ApiController]
public sealed class AdminDashboardController(
    IDashboardService dashboardService,
    ILogger<AdminDashboardController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardResponse>> Get()
    {
        try
        {
            return Ok(await dashboardService.GetAsync());
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // A canceled request is not a server failure.
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to retrieve the admin dashboard.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Unable to load the dashboard. Please try again later." });
        }
    }
}
