using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Admin.RegionalMinimumWages;
using UI_Admin.Services;
using UI_Admin.Services.Admin;

namespace UI_Admin.Controllers.Admin;

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
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken = default)
    {
        var items = await _apiService.GetAllAsync(search, status, cancellationToken);

        if (TempData["SuccessMessage"] is string success)
        {
            ViewBag.SuccessMessage = success;
        }

        if (TempData["ErrorMessage"] is string error)
        {
            ViewBag.ErrorMessage = error;
        }

        var viewModel = new RegionalMinimumWageListViewModel
        {
            Items = items,
            Search = search,
            StatusFilter = status ?? "all"
        };

        return View(viewModel);
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

        if (TempData["ErrorMessage"] is string error)
        {
            ViewBag.ErrorMessage = error;
        }

        return View(detail);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var activeWages = await _apiService.GetAllAsync(status: "ACTIVE", cancellationToken: cancellationToken);
        var model = new CreateRegionalMinimumWageViewModel
        {
            ActiveWages = activeWages
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRegionalMinimumWageViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.ActiveWages = await _apiService.GetAllAsync(status: "ACTIVE", cancellationToken: cancellationToken);
            return View(model);
        }

        var (success, data, errorMessage, isConflict) = await _apiService.CreateAsync(model, cancellationToken);
        if (success && data != null)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage '{data.WageName}' created in DRAFT status successfully.";
            return RedirectToAction(nameof(Details), new { id = data.RegionalMinimumWageId });
        }

        if (isConflict)
        {
            ModelState.AddModelError(string.Empty, "Regional minimum wage conflict detected.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create regional minimum wage.");
        }

        model.ActiveWages = await _apiService.GetAllAsync(status: "ACTIVE", cancellationToken: cancellationToken);
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

        if (!detail.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Only draft regional minimum wages can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var activeWages = await EnsureReplacedWageIncludedAsync(detail.ReplacedWageId, cancellationToken);

        var editModel = new UpdateRegionalMinimumWageViewModel
        {
            RegionalMinimumWageId = detail.RegionalMinimumWageId,
            WageName = detail.WageName,
            MinimumHourlyRate = detail.MinimumHourlyRate,
            Description = detail.Description,
            ReplacedWageId = detail.ReplacedWageId,
            EffectiveFrom = detail.EffectiveFrom,
            Status = detail.Status,
            ActiveWages = activeWages
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
            model.ActiveWages = await EnsureReplacedWageIncludedAsync(model.ReplacedWageId, cancellationToken);
            return View(model);
        }

        var (success, data, errorMessage, isConflict, isNotFound) = await _apiService.UpdateAsync(id, model, cancellationToken);
        if (success && data != null)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage '{data.WageName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = data.RegionalMinimumWageId });
        }

        if (isConflict)
        {
            ModelState.AddModelError(string.Empty, !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : "Only draft regional minimum wages can be edited.");
        }
        else if (isNotFound)
        {
            TempData["ErrorMessage"] = "Regional minimum wage not found.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update regional minimum wage.");
        }

        model.ActiveWages = await EnsureReplacedWageIncludedAsync(model.ReplacedWageId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, data, errorMessage) = await _apiService.PublishAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage #{id} published successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to publish regional minimum wage.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, data, errorMessage) = await _apiService.CancelAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Regional minimum wage #{id} cancelled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to cancel regional minimum wage.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<RegionalMinimumWageListItemViewModel>> EnsureReplacedWageIncludedAsync(int? replacedWageId, CancellationToken cancellationToken)
    {
        var activeWages = await _apiService.GetAllAsync(status: "ACTIVE", cancellationToken: cancellationToken);
        if (replacedWageId.HasValue && !activeWages.Any(w => w.RegionalMinimumWageId == replacedWageId.Value))
        {
            var replaced = await _apiService.GetByIdAsync(replacedWageId.Value, cancellationToken);
            if (replaced != null)
            {
                activeWages.Add(new RegionalMinimumWageListItemViewModel
                {
                    RegionalMinimumWageId = replaced.RegionalMinimumWageId,
                    WageName = replaced.WageName,
                    MinimumHourlyRate = replaced.MinimumHourlyRate,
                    Description = replaced.Description,
                    Status = replaced.Status,
                    EffectiveFrom = replaced.EffectiveFrom,
                    EffectiveTo = replaced.EffectiveTo,
                    ReplacedWageId = replaced.ReplacedWageId
                });
            }
        }
        return activeWages;
    }
}
