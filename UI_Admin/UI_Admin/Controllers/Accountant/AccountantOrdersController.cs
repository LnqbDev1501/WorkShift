using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;
using UI_Admin.Services.Admin;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT,ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountantOrdersController(AccountantOrderApiService api, BranchApiService branchApi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, int? branchId, string? status, string? paymentStatus,
        DateTime? dateFrom, DateTime? dateTo, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new AccountantOrderListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = 10,
            Search = search,
            BranchId = branchId,
            StatusFilter = status,
            PaymentStatusFilter = paymentStatus,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Branches = (await branchApi.GetActiveBranchesAsync(cancellationToken))
                .Select(b => new BranchOptionViewModel { Id = b.Id, Name = b.Name })
                .ToList()
        };

        try
        {
            var paged = await api.GetPagedAsync(search, branchId, status, paymentStatus, dateFrom, dateTo, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            ModelState.AddModelError(string.Empty, "Unable to load orders: " + ex.Message);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var order = await api.GetByIdAsync(id, cancellationToken);
            return View(order);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var order = await api.GetByIdAsync(id, cancellationToken);
            return View(order);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportInvoice(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var pdf = await api.GetInvoicePdfAsync(id, cancellationToken);
            return File(pdf, "application/pdf", $"invoice_{id}.pdf");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "AccountantOrders") });
    }
}
