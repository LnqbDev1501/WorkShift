using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public class ManagerDashboardController(ManagerDashboardApiService dashboard, ILogger<ManagerDashboardController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new ManagerDashboardViewModel();
        try
        {
            model.Data = await dashboard.GetAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Unable to load the manager dashboard.");
            model.ErrorMessage = "Unable to load your branch dashboard. Please try again shortly.";
        }
        return View(model);
    }
}
