using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant;
using UI_Admin.Services.Accountant;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ACCOUNTANT,ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountantCategoriesController(AccountantCategoryApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new AccountantCategoryListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = ListPaginationViewModel.DefaultPageSize,
            Search = search,
            StatusFilter = status
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
            ModelState.AddModelError(string.Empty, "Unable to load categories: " + ex.Message);
        }

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
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "AccountantCategories") });
    }
}