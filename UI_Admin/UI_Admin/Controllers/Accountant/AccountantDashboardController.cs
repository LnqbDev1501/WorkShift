using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UI_Admin.Models.Accountant;
using UI_Admin.Services.Accountant;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT")]
public class AccountantDashboardController(AccountantDashboardApiService dashboard, ILogger<AccountantDashboardController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new AccountantDashboardViewModel();
        try
        {
            model.Data = await dashboard.GetAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Unable to load the accountant dashboard.");
            model.ErrorMessage = "Không thể tải dữ liệu tổng quan. Vui lòng thử lại sau.";
        }
        return View(model);
    }
}
