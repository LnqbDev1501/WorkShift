using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Catalog;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ManagerCategoriesController(CategoryApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new CategoryListPagedViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = 10,
            Search = search,
            StatusFilter = status ?? "all"
        };

        try
        {
            var paged = await api.GetPagedAsync(search, status, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
            model.StatusFilter = paged.StatusFilter;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, "Unable to load categories: " + ex.Message);
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
            var category = await api.GetByIdAsync(id, cancellationToken);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateCategoryViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, errorMessage) = await api.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Category '{model.CategoryName.Trim()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create category.");
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
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new EditCategoryViewModel
            {
                CategoryId = detail.CategoryId,
                CategoryName = detail.CategoryName,
                Description = detail.Description,
                Status = detail.Status,
                ProductCount = detail.ProductCount
            });
        }
        catch (HttpRequestException)
        {
            TempData["ErrorMessage"] = "Category could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.CategoryId) return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var (success, errorMessage) = await api.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Category '{model.CategoryName.Trim()}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update category.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await api.DeactivateAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Category deactivated successfully." : (errorMessage ?? "Failed to deactivate category.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await api.RestoreAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Category restored successfully." : (errorMessage ?? "Failed to restore category.");

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "ManagerCategories") });
    }
}
