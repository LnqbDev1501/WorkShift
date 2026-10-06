using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
public class ProductsController : Controller
{
    private const string WriteRoles = "MANAGER";
    private readonly ProductApiService _productApiService;
    private readonly CategoryApiService _categoryApiService;
    private readonly UserApiService _userApiService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductApiService productApiService,
        CategoryApiService categoryApiService,
        UserApiService userApiService,
        ILogger<ProductsController> logger)
    {
        _productApiService = productApiService;
        _categoryApiService = categoryApiService;
        _userApiService = userApiService;
        _logger = logger;
    }

    private bool IsManager => User.IsInRole("MANAGER");

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] int? branchId,
        [FromQuery] int? categoryId,
        [FromQuery] string? status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = ListPaginationViewModel.DefaultPageSize;

        // Managers are scoped to their own branch server-side; the filter is only for Admin/Accountant.
        var effectiveBranchId = IsManager ? null : branchId;
        var viewModel = await _productApiService.GetPagedAsync(search, effectiveBranchId, categoryId, status, page, pageSize, cancellationToken);
        viewModel.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        if (!IsManager)
        {
            try
            {
                var branches = await _userApiService.GetActiveBranchesAsync(cancellationToken);
                viewModel.Branches = branches
                    .Select(branch => new BranchOptionItemViewModel { Id = branch.Id, Name = branch.Name })
                    .OrderBy(branch => branch.Name)
                    .ToList();
            }
            catch
            {
                viewModel.Branches = [];
            }
        }

        if (TempData["SuccessMessage"] is string success)
            viewModel.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            viewModel.ErrorMessage = error;

        return View("Index", viewModel);
    }

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int? branchId,
        [FromQuery] int? categoryId,
        [FromQuery] string? status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return Index(search, branchId, categoryId, status, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _productApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }

    [HttpGet]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        try
        {
            return View(new CreateProductViewModel
            {
                Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken)
            });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Unable to load categories for product creation.");
            TempData["ErrorMessage"] = "Categories could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _productApiService.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Product '{model.ProductCode.Trim().ToUpperInvariant()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create product.");
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        ProductDetailViewModel? detail;
        try
        {
            detail = await _productApiService.GetByIdAsync(id, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Unable to load product {ProductId} for editing.", id);
            TempData["ErrorMessage"] = "Product could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        if (detail == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        List<CategoryOptionViewModel> categories;
        try
        {
            categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Unable to load categories for product editing.");
            TempData["ErrorMessage"] = "Categories could not be loaded. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        return View(new EditProductViewModel
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

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditProductViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.ProductId) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _productApiService.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Product '{model.ProductCode}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update product.");
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _productApiService.DeactivateAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Product deactivated successfully." : (errorMessage ?? "Failed to deactivate product.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = WriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _productApiService.RestoreAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Product restored successfully." : (errorMessage ?? "Failed to restore product.");

        return RedirectToAction(nameof(Index));
    }
}
