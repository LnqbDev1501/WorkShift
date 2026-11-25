using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers;

[Authorize(Roles = "MANAGER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ManagerProductsController(ManagerProductApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string? status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new ManagerProductListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = 10,
            Search = search,
            CategoryId = categoryId,
            StatusFilter = status ?? "all"
        };

        try
        {
            var paged = await api.GetPagedAsync(search, categoryId, status, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
            model.Categories = paged.Categories;
            model.StatusFilter = paged.StatusFilter;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, "Unable to load products: " + ex.Message);
        }

        if (TempData["SuccessMessage"] is string success)
            model.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            model.ErrorMessage = error;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var product = await api.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        try
        {
            return View(new ManagerCreateProductViewModel
            {
                Categories = await api.GetActiveCategoriesAsync(cancellationToken)
            });
        }
        catch (HttpRequestException)
        {
            TempData["ErrorMessage"] = "Categories could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManagerCreateProductViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await api.GetActiveCategoriesAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await api.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Product '{model.ProductCode.Trim().ToUpperInvariant()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await api.GetActiveCategoriesAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create product.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        try
        {
            var detail = await api.GetByIdAsync(id, cancellationToken);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            var categories = await api.GetActiveCategoriesAsync(cancellationToken);

            return View(new ManagerEditProductViewModel
            {
                ProductId = detail.ProductId,
                CategoryId = detail.CategoryId,
                ProductCode = detail.ProductCode,
                ProductName = detail.ProductName,
                Price = detail.Price,
                Description = detail.Description,
                ImageUrl = detail.ImageUrl,
                Status = detail.Status,
                Categories = categories
            });
        }
        catch (HttpRequestException)
        {
            TempData["ErrorMessage"] = "Product could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ManagerEditProductViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.ProductId) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.Categories = await api.GetActiveCategoriesAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await api.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Product '{model.ProductCode}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await api.GetActiveCategoriesAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update product.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await api.DeactivateAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Product deactivated successfully." : (errorMessage ?? "Failed to deactivate product.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await api.RestoreAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Product restored successfully." : (errorMessage ?? "Failed to restore product.");

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "ManagerProducts") });
    }
}