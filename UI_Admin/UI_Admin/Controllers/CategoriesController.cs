using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
public class CategoriesController : Controller
{
    private const string WriteRoles = "ADMIN,MANAGER";
    private readonly CategoryApiService _categoryApiService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(CategoryApiService categoryApiService, ILogger<CategoriesController> logger)
    {
        _categoryApiService = categoryApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] string? status = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = ListPaginationViewModel.DefaultPageSize;

        var viewModel = await _categoryApiService.GetPagedAsync(search, status, page, pageSize, cancellationToken);

        if (TempData["SuccessMessage"] is string success)
            viewModel.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            viewModel.ErrorMessage = error;

        return View("Index", viewModel);
    }

    [HttpGet]
    public Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? status = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        return Index(search, status, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _categoryApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }

    [HttpGet]
    [Authorize(Roles = WriteRoles)]
    public IActionResult Create() => View(new CreateCategoryViewModel());

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, errorMessage) = await _categoryApiService.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Category '{model.CategoryName.Trim()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create category.");
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _categoryApiService.GetByIdAsync(id, cancellationToken);
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

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.CategoryId) return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var (success, errorMessage) = await _categoryApiService.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Category '{model.CategoryName.Trim()}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update category.");
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _categoryApiService.DeactivateAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Category deactivated successfully." : (errorMessage ?? "Failed to deactivate category.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _categoryApiService.RestoreAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Category restored successfully." : (errorMessage ?? "Failed to restore category.");

        return RedirectToAction(nameof(Index));
    }
}
