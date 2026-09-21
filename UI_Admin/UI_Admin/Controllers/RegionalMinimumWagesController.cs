using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.RegionalMinimumWages;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class RegionalMinimumWagesController : Controller
{
    private readonly RegionalMinimumWageApiService _apiService;
    private readonly ILogger<RegionalMinimumWagesController> _logger;

    public RegionalMinimumWagesController(
        RegionalMinimumWageApiService apiService,
        ILogger<RegionalMinimumWagesController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var items = await _apiService.GetAllAsync(cancellationToken);

        if (TempData["SuccessMessage"] is string success)
        {
            ViewBag.SuccessMessage = success;
        }

        if (TempData["ErrorMessage"] is string error)
        {
            ViewBag.ErrorMessage = error;
        }

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _apiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Regional minimum wage not found.";
            return RedirectToAction(nameof(Index));
        }

        if (TempData["SuccessMessage"] is string success)
        {
            ViewBag.SuccessMessage = success;
        }

        return View(detail);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateRegionalMinimumWageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRegionalMinimumWageViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, data, errorMessage, isConflict) = await _apiService.CreateAsync(model, cancellationToken);
        if (success && data != null)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage '{data.WageName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = data.RegionalMinimumWageId });
        }

        if (isConflict)
        {
            ModelState.AddModelError(string.Empty, "Regional minimum wage name already exists.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create regional minimum wage.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _apiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Regional minimum wage not found.";
            return RedirectToAction(nameof(Index));
        }

        var editModel = new UpdateRegionalMinimumWageViewModel
        {
            RegionalMinimumWageId = detail.RegionalMinimumWageId,
            WageName = detail.WageName,
            MinimumHourlyRate = detail.MinimumHourlyRate,
            Description = detail.Description
        };

        return View(editModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateRegionalMinimumWageViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.RegionalMinimumWageId) return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, data, errorMessage, isConflict, isNotFound) = await _apiService.UpdateAsync(id, model, cancellationToken);
        if (success && data != null)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage updated successfully. A new version (#{data.RegionalMinimumWageId}) has been created.";
            return RedirectToAction(nameof(Details), new { id = data.RegionalMinimumWageId });
        }

        if (isConflict)
        {
            ModelState.AddModelError(string.Empty, !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : "This wage is no longer the current version and cannot be updated.");
        }
        else if (isNotFound)
        {
            ModelState.AddModelError(string.Empty, "Regional minimum wage not found.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update regional minimum wage.");
        }

        return View(model);
    }
}
