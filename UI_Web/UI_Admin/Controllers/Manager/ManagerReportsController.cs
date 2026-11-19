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
    public async Task<IActionResult> BranchSales(CancellationToken ct = default)
    {
        try
        {
            var summary = await api.GetBranchSalesSummaryAsync(null, null, ct);
            ViewBag.BranchName = summary.BranchName;
        }
        catch
        {
            ViewBag.BranchName = "Assigned Branch";
        }
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CountOrders(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        try
        {
            var summary = await api.GetBranchSalesSummaryAsync(dateFrom, dateTo, ct);
            return Json(new { success = true, totalOrders = summary.TotalOrders, branchName = summary.BranchName, message = $"{summary.TotalOrders} orders have been found." });
        }
        catch (HttpRequestException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
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
