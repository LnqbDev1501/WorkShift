using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.EmployeeTypes;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class EmployeeTypesController : Controller
{
    private readonly EmployeeTypeApiService _employeeTypeApiService;
    private readonly ILogger<EmployeeTypesController> _logger;

    public EmployeeTypesController(EmployeeTypeApiService employeeTypeApiService, ILogger<EmployeeTypesController> logger)
    {
        _employeeTypeApiService = employeeTypeApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status = "all", int page = 1, int pageSize = ListPaginationViewModel.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var model = new EmployeeTypeListPagedViewModel
        {
            Search = search,
            StatusFilter = status?.Trim().ToLowerInvariant() ?? "all",
            PageNumber = Math.Clamp(page, 1, int.MaxValue / 100),
            PageSize = ListPaginationViewModel.DefaultPageSize
        };
        try
        {
            model = await _employeeTypeApiService.GetPagedAsync(search, status, model.PageNumber, model.PageSize, cancellationToken);
            if (model.PageNumber > Math.Max(1, model.TotalPages))
                return RedirectToAction(nameof(Index), new { search, status, page = Math.Max(1, model.TotalPages), pageSize = model.PageSize });
        }
        catch (HttpRequestException exception) { model.ErrorMessage = GetErrorMessage(exception); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { model.ErrorMessage = "The server took too long to respond. Please try again."; }
        return View(model);
    }

    [HttpGet]
    public Task<IActionResult> List(string? search, string? status = "all", int page = 1, int pageSize = ListPaginationViewModel.DefaultPageSize, CancellationToken cancellationToken = default)
        => Index(search, status, page, pageSize, cancellationToken);

    [HttpGet]
    public IActionResult Create() => View(new EmployeeTypeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeTypeFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, errorMessage) = await RunMutationAsync(() => _employeeTypeApiService.CreateAsync(model, cancellationToken), cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create employee type.");
            return View(model);
        }
        TempData["SuccessMessage"] = $"Employee type '{model.TypeName.Trim()}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var detail = await LoadDetailAsync(id, cancellationToken);
        if (detail == null) return RedirectToAction(nameof(Index));
        return View(new EditEmployeeTypeViewModel
        {
            EmployeeTypeId = detail.EmployeeTypeId,
            TypeName = detail.TypeName,
            Description = detail.Description,
            Status = detail.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditEmployeeTypeViewModel model, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || id != model.EmployeeTypeId) return BadRequest();
        var detail = await LoadDetailAsync(id, cancellationToken);
        if (detail == null) return RedirectToAction(nameof(Index));
        model.Status = detail.Status;
        if (!ModelState.IsValid) return View(model);
        var (success, errorMessage) = await RunMutationAsync(() => _employeeTypeApiService.UpdateAsync(id, model, cancellationToken), cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update employee type.");
            return View(model);
        }
        TempData["SuccessMessage"] = $"Employee type '{model.TypeName.Trim()}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await LoadDetailAsync(id, cancellationToken);
        return model == null ? RedirectToAction(nameof(Index)) : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();
        var (success, errorMessage) = await RunMutationAsync(() => _employeeTypeApiService.DeactivateAsync(id, cancellationToken), cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Employee type deactivated successfully." : errorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();
        var (success, errorMessage) = await RunMutationAsync(() => _employeeTypeApiService.RestoreAsync(id, cancellationToken), cancellationToken);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Employee type restored successfully." : errorMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeeTypeDetailViewModel?> LoadDetailAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = id > 0 ? await _employeeTypeApiService.GetByIdAsync(id, cancellationToken) : null;
            if (model == null) TempData["ErrorMessage"] = "Employee type not found.";
            return model;
        }
        catch (HttpRequestException exception) { TempData["ErrorMessage"] = GetErrorMessage(exception); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { TempData["ErrorMessage"] = "The server took too long to respond. Please try again."; }
        return null;
    }

    private async Task<(bool Success, string? ErrorMessage)> RunMutationAsync(Func<Task<(bool Success, string? ErrorMessage)>> action, CancellationToken cancellationToken)
    {
        try { return await action(); }
        catch (HttpRequestException exception) { return (false, GetErrorMessage(exception)); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, "The server took too long to respond. Check the employee type list before trying again.");
        }
    }

    private string GetErrorMessage(HttpRequestException exception)
    {
        _logger.LogWarning(exception, "Employee type API request failed");
        return exception.StatusCode.HasValue ? exception.Message : "Unable to connect to the server. Please try again.";
    }
}
