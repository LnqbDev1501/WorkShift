using System;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Staff.Orders;
using UI_Admin.Services.Staff;


namespace UI_Admin.Controllers.Staff;

[Authorize(Roles = "STAFF")]
public class StaffOrdersController(StaffOrderApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page = 1,
        CancellationToken ct = default)
    {
        var model = new StaffOrderListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = ListPaginationViewModel.DefaultPageSize,
            Search = search,
            StatusFilter = status,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        try
        {
            var paged = await api.GetMyOrdersAsync(search, status, dateFrom, dateTo, model.PageNumber, model.PageSize, ct);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
            model.TotalPages = paged.TotalPages;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            model.ErrorMessage = ex.Message;
        }

        if (TempData["SuccessMessage"] is string success)
            model.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            model.ErrorMessage = error;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        try
        {
            var products = await api.GetActiveProductsAsync(ct);
            return View(new StaffCreateOrderViewModel
            {
                Products = products
            });
        }
        catch (HttpRequestException)
        {
            TempData["ErrorMessage"] = "Products could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffCreateOrderViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            model.Products = await api.GetActiveProductsAsync(ct);
            return View(model);
        }

        if (model.Items == null || model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Order must contain at least one item.");
            model.Products = await api.GetActiveProductsAsync(ct);
            return View(model);
        }

        try
        {
            var result = await api.CreateOrderAsync(model, ct);
            TempData["SuccessMessage"] = $"Order '{result.OrderNumber}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = result.OrderId });
        }
        catch (HttpRequestException exception)
        {
            model.Products = await api.GetActiveProductsAsync(ct);
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var order = await api.GetOrderAsync(id, ct);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CancellationToken ct = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            await api.CompleteOrderAsync(id, ct);
            TempData["SuccessMessage"] = "Order completed successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            await api.CancelOrderAsync(id, ct);
            TempData["SuccessMessage"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(Details), new { id });
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
        await HttpContext.SignOutAsync("WorkShift.Admin");
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "StaffOrders") });
    }
}
