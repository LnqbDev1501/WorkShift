using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Accountant;
using UI_Admin.Services.Accountant;


namespace UI_Admin.Controllers;

[Authorize(Roles = "ACCOUNTANT,ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountantProductsController(AccountantProductApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? branchId,
        int? categoryId,
        string? status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (!User.IsInRole("ADMIN"))
        {
            branchId = null;
        }

        var model = new AccountantProductListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = ListPaginationViewModel.DefaultPageSize,
            Search = search,
            BranchId = branchId,
            CategoryId = categoryId,
            StatusFilter = status ?? "all"
        };

        try
        {
            var paged = await api.GetPagedAsync(search, branchId, categoryId, status, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
            model.Categories = paged.Categories;
            model.Branches = paged.Branches;
            model.StatusFilter = paged.StatusFilter;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            ModelState.AddModelError(string.Empty, "Unable to load products: " + ex.Message);
        }

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
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "AccountantProducts") });
    }
}
