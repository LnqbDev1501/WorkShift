using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "MANAGER")]
public class ManagerStaffController : Controller
{
    private readonly ManagerStaffApiService _managerStaffApiService;
    private readonly CategoryApiService _categoryApiService;
    private readonly ILogger<ManagerStaffController> _logger;

    public ManagerStaffController(
        ManagerStaffApiService managerStaffApiService,
        CategoryApiService categoryApiService,
        ILogger<ManagerStaffController> logger)
    {
        _managerStaffApiService = managerStaffApiService;
        _categoryApiService = categoryApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] string? status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = ListPaginationViewModel.DefaultPageSize;

        var viewModel = await _managerStaffApiService.GetStaffListAsync(search, status, page, pageSize, cancellationToken);

        if (TempData["SuccessMessage"] is string success)
            viewModel.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            viewModel.ErrorMessage = error;

        return View("Index", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListPaginationViewModel.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return await Index(search, status, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var detail = await _managerStaffApiService.GetStaffDetailAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Staff account not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        return View(new CreateStaffAccountViewModel
        {
            Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken),
            EmployeeTypes = await _managerStaffApiService.GetActiveEmployeeTypeOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffAccountViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
            model.EmployeeTypes = await _managerStaffApiService.GetActiveEmployeeTypeOptionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _managerStaffApiService.CreateStaffAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Staff account '{model.Email}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        model.EmployeeTypes = await _managerStaffApiService.GetActiveEmployeeTypeOptionsAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create staff account.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return NotFound();

        var detail = await _managerStaffApiService.GetStaffDetailAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Staff account not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(new EditStaffAccountViewModel
        {
            UserId = detail.UserId,
            FullName = detail.FullName,
            Email = detail.Email,
            Phone = detail.Phone,
            IdentityNo = detail.IdentityNo,
            Gender = detail.Gender,
            DateOfBirth = detail.DateOfBirth,
            Address = detail.Address ?? string.Empty,
            Status = detail.Status,
            Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditStaffAccountViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.UserId) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _managerStaffApiService.UpdateStaffAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Staff account '{model.Email}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Categories = await _categoryApiService.GetActiveOptionsAsync(cancellationToken);
        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update staff account.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var (success, errorMessage) = await _managerStaffApiService.DeactivateStaffAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Staff account deactivated successfully." : (errorMessage ?? "Failed to deactivate staff account.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken = default)
    {
        var (success, errorMessage) = await _managerStaffApiService.RestoreStaffAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Staff account restored successfully." : (errorMessage ?? "Failed to restore staff account.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendSetupLink(Guid id, CancellationToken cancellationToken = default)
    {
        var (success, errorMessage) = await _managerStaffApiService.ResendSetupLinkAsync(id, cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Setup link has been resent to the staff email." : (errorMessage ?? "Failed to resend setup link.");

        return RedirectToAction(nameof(Index));
    }
}