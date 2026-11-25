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
public class ManagerCategoriesController(ManagerCategoryApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new ManagerCategoryListViewModel
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

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "ManagerCategories") });
    }
}