using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Admin.EmployeeTypes;
using UI_Admin.Services;
using UI_Admin.Services.Admin;

namespace UI_Admin.Controllers.Admin;

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
            model.IsLoaded = true;
            if (model.PageNumber > Math.Max(1, model.TotalPages))
                return RedirectToAction(nameof(Index), new { search, status, page = Math.Max(1, model.TotalPages), pageSize = model.PageSize });
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Unable to load employee types.");
            model.ErrorMessage = "Unable to load employee types. Please try again.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
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
        var result = await RunMutationAsync(() => _employeeTypeApiService.CreateAsync(model, cancellationToken), cancellationToken);
        if (!result.Success)
        {
            AddMutationErrors(result, "Failed to create employee type.");
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
        var result = await RunMutationAsync(() => _employeeTypeApiService.UpdateAsync(id, model, cancellationToken), cancellationToken);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["ErrorMessage"] = "Employee type not found.";
            return RedirectToAction(nameof(Index));
        }
        if (!result.Success)
        {
            AddMutationErrors(result, "Failed to update employee type.");
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

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> StatusConfirmation(int id, string? operation, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || operation is not ("deactivate" or "restore"))
            return BadRequest(new { message = "Invalid employee type action." });

        try
        {
            var detail = await _employeeTypeApiService.GetByIdAsync(id, cancellationToken);
            if (detail is null) return NotFound(new { message = "Employee type not found." });
            if (detail.EmployeeTypeId != id || string.IsNullOrWhiteSpace(detail.TypeName) ||
                detail.Status is not ("ACTIVE" or "INACTIVE"))
                throw new JsonException("The server returned invalid employee type details.");

            if (operation == "deactivate" && detail.Status == "INACTIVE")
                return Conflict(new { message = "The employee type is already inactive." });
            if (operation == "restore" && detail.Status == "ACTIVE")
                return Conflict(new { message = "The employee type is already active." });

            return Json(new { employeeTypeId = detail.EmployeeTypeId, typeName = detail.TypeName, status = detail.Status });
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Unable to load employee type {Id} for status confirmation.", id);
            if (exception is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized })
                return Unauthorized(new { message = "Your session has expired. Please sign in again." });
            if (exception is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Forbidden })
                return StatusCode(403, new { message = "You do not have permission to change employee type status." });
            return StatusCode(503, new { message = "Unable to load employee type details. Please try again." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();
        var result = await RunMutationAsync(() => _employeeTypeApiService.DeactivateAsync(id, cancellationToken), cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Employee type deactivated successfully."
            : result.ErrorMessage ?? "Unable to deactivate employee type. Please try again.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();
        var result = await RunMutationAsync(() => _employeeTypeApiService.RestoreAsync(id, cancellationToken), cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Employee type restored successfully."
            : result.ErrorMessage ?? "Unable to restore employee type. Please try again.";
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

    private void AddMutationErrors(EmployeeTypeApiResult result, string fallbackMessage)
    {
        var added = false;
        foreach (var (field, messages) in result.FieldErrors)
        {
            // API validation may use CLR names, JSON names or paths such as $.typeName.
            var leaf = field.Split('.').Last();
            var key = leaf.Equals(nameof(EmployeeTypeFormViewModel.TypeName), StringComparison.OrdinalIgnoreCase)
                ? nameof(EmployeeTypeFormViewModel.TypeName)
                : leaf.Equals(nameof(EmployeeTypeFormViewModel.Description), StringComparison.OrdinalIgnoreCase)
                    ? nameof(EmployeeTypeFormViewModel.Description) : string.Empty;
            foreach (var message in messages ?? [])
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                ModelState.AddModelError(key, message);
                added = true;
            }
        }
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            var key = result.StatusCode == HttpStatusCode.Conflict && result.ErrorMessage == "Employee type name already exists."
                ? nameof(EmployeeTypeFormViewModel.TypeName) : string.Empty;
            ModelState.AddModelError(key, result.ErrorMessage);
            added = true;
        }
        if (!added) ModelState.AddModelError(string.Empty, fallbackMessage);
    }

    private async Task<EmployeeTypeApiResult> RunMutationAsync(Func<Task<EmployeeTypeApiResult>> action, CancellationToken cancellationToken)
    {
        try { return await action(); }
        catch (HttpRequestException exception)
        {
            return new EmployeeTypeApiResult { StatusCode = exception.StatusCode, ErrorMessage = GetErrorMessage(exception) };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new EmployeeTypeApiResult { ErrorMessage = "The server took too long to respond. Check the employee type list before trying again." };
        }
    }

    private string GetErrorMessage(HttpRequestException exception)
    {
        _logger.LogWarning(exception, "Employee type API request failed");
        return exception.StatusCode.HasValue ? exception.Message : "Unable to connect to the server. Please try again.";
    }
}
