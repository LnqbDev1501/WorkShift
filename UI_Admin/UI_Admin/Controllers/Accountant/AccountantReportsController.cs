using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ACCOUNTANT,ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountantReportsController(AccountantOrderApiService api, BranchApiService branchApi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> BranchSales(CancellationToken ct = default)
    {
        var branches = await branchApi.GetActiveBranchesAsync(ct);
        var model = new BranchSalesReportViewModel
        {
            Branches = branches.Select(b => new BranchOptionViewModel { Id = b.Id, Name = b.Name }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportBranchSales(
        int? branchId, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        if (branchId is null || branchId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a branch to export the sales report.";
            return RedirectToAction(nameof(BranchSales));
        }

        try
        {
            var bytes = await api.ExportBranchSalesReportAsync(branchId, dateFrom, dateTo, cancellationToken);
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, $"branch_sales_{branchId}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
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