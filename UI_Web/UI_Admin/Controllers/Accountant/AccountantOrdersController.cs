using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Accountant;
using UI_Admin.Services;


namespace UI_Admin.Controllers;

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
        if (!User.IsInRole("ADMIN"))
        {
            branchId = null;
        }

        // Default to current week (Monday - Sunday, VN time UTC+7) when date range is not specified
        if (!dateFrom.HasValue && !dateTo.HasValue)
        {
            var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            int diff = (7 + (int)todayVn.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var currentMonday = todayVn.AddDays(-diff);
            var currentSunday = currentMonday.AddDays(6);
            dateFrom = currentMonday.ToDateTime(TimeOnly.MinValue);
            dateTo = currentSunday.ToDateTime(TimeOnly.MinValue);
        }
        else if (dateFrom.HasValue && !dateTo.HasValue)
        {
            var start = DateOnly.FromDateTime(dateFrom.Value);
            dateTo = start.AddDays(6).ToDateTime(TimeOnly.MinValue);
        }

        var branches = User.IsInRole("ADMIN")
            ? (await branchApi.GetActiveBranchesAsync(cancellationToken))
                .Select(b => new BranchOptionViewModel { Id = b.Id, Name = b.Name })
                .ToList()
            : [];

        var model = new AccountantOrderListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = ListPaginationViewModel.DefaultPageSize,
            Search = search,
            BranchId = branchId,
            StatusFilter = status,
            PaymentStatusFilter = paymentStatus,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Branches = branches
        };

        try
        {
            var paged = await api.GetPagedAsync(search, branchId, status, paymentStatus, dateFrom, dateTo, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
            model.PaidCount = paged.PaidCount;
            model.CompletedCount = paged.CompletedCount;
            model.TotalOrderValue = paged.TotalOrderValue;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            ModelState.AddModelError(string.Empty, "Unable to load orders: " + ex.Message);
        }

        return View("~/Views/AccountantOrders/Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var order = await api.GetByIdAsync(id, cancellationToken);
            return View("~/Views/AccountantOrders/Details.cshtml", order);
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
            return View("~/Views/AccountantOrders/Invoice.cshtml", order);
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
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index)) });
    }
}
