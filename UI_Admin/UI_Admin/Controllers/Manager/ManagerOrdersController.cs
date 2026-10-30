using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "MANAGER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ManagerOrdersController(ManagerOrderApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, string? status, string? paymentStatus,
        DateTime? dateFrom, DateTime? dateTo, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new ManagerOrderListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = ListPaginationViewModel.DefaultPageSize,
            Search = search,
            StatusFilter = status,
            PaymentStatusFilter = paymentStatus,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        try
        {
            var paged = await api.GetPagedAsync(search, status, paymentStatus, dateFrom, dateTo, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
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
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            await api.CompleteOrderAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Order completed successfully.";
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            await api.CancelOrderAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Order cancelled successfully.";
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "ManagerOrders") });
    }
}