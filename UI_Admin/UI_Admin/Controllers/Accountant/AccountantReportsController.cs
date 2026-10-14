using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ACCOUNTANT,ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountantReportsController(AccountantOrderApiService api) : Controller
{
    [HttpGet]
    public IActionResult BranchSales()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportBranchSales(
        int? branchId, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var csv = await api.ExportBranchSalesReportAsync(branchId, dateFrom, dateTo, cancellationToken);
            return File(csv, "text/csv", $"branch_sales_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(BranchSales));
        }
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(BranchSales), "AccountantReports") });
    }
}