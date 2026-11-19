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

        if (!User.IsInRole("ADMIN"))
        {
            try
            {
                var summary = await api.GetBranchSalesSummaryAsync(null, null, null, ct);
                model.BranchName = summary.BranchName;
                ViewBag.BranchName = summary.BranchName;
            }
            catch
            {
                model.BranchName = "Assigned Branch";
                ViewBag.BranchName = "Assigned Branch";
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CountOrders(int? branchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        try
        {
            var summary = await api.GetBranchSalesSummaryAsync(branchId, dateFrom, dateTo, ct);
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
    public async Task<IActionResult> GetAnalytics(int? branchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        try
        {
            var analytics = await api.GetBranchSalesAnalyticsAsync(branchId, dateFrom, dateTo, ct);
            return Json(new { success = true, data = analytics });
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
        int? branchId, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        if (User.IsInRole("ADMIN"))
        {
            if (branchId is null || branchId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a branch to export the sales report.";
                return RedirectToAction(nameof(BranchSales));
            }
        }
        else
        {
            branchId = null;
        }

        try
        {
            var bytes = await api.ExportBranchSalesReportAsync(branchId, dateFrom, dateTo, cancellationToken);
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, $"branch_sales_{DateTime.UtcNow:yyyyMMdd}.xlsx");
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