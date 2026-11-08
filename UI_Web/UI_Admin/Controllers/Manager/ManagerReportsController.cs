using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers;

[Authorize(Roles = "MANAGER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ManagerReportsController(ManagerReportApiService api) : Controller
{
    [HttpGet]
    public IActionResult BranchSales()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportBranchSales(
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await api.ExportBranchSalesReportAsync(dateFrom, dateTo, cancellationToken);
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, $"branch_sales_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(BranchSales));
        }
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(BranchSales), "ManagerReports") });
    }
}
