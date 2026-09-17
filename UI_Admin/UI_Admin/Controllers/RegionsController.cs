using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Regions;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class RegionsController : Controller
{
    private readonly RegionApiService _regionApiService;
    private readonly BranchApiService _branchApiService;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(RegionApiService regionApiService, BranchApiService branchApiService, ILogger<RegionsController> logger)
    {
        _regionApiService = regionApiService;
        _branchApiService = branchApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] string? status = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var viewModel = await _regionApiService.GetPagedAsync(search, status, page, pageSize, cancellationToken);

        if (TempData["SuccessMessage"] is string success)
        {
            viewModel.SuccessMessage = success;
        }

        if (TempData["ErrorMessage"] is string error)
        {
            viewModel.ErrorMessage = error;
        }

        return View("Index", viewModel);
    }

    [HttpGet]
    public Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? status = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return Index(search, status, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _regionApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Region not found.";
            return RedirectToAction(nameof(Index));
        }

        if (detail.Branches.Any(b => !b.Latitude.HasValue || !b.Longitude.HasValue))
        {
            var branchTasks = detail.Branches.Select(async b =>
            {
                if (!b.Latitude.HasValue || !b.Longitude.HasValue)
                {
                    var bDetail = await _branchApiService.GetByIdAsync(b.BranchId, cancellationToken);
                    if (bDetail != null)
                    {
                        b.Address = string.IsNullOrWhiteSpace(b.Address) ? bDetail.Address : b.Address;
                        b.Latitude = bDetail.Latitude;
                        b.Longitude = bDetail.Longitude;
                    }
                }
            });
            await Task.WhenAll(branchTasks);
        }

        return View(detail);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateRegionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRegionViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, errorMessage) = await _regionApiService.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Region '{model.RegionCode.Trim().ToUpperInvariant()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create region.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _regionApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Region not found.";
            return RedirectToAction(nameof(Index));
        }

        var editModel = new EditRegionViewModel
        {
            RegionId = detail.RegionId,
            RegionCode = detail.RegionCode,
            RegionName = detail.RegionName,
            Description = detail.Description,
            Status = detail.Status,
            BranchCount = detail.Branches?.Count ?? 0
        };

        return View(editModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditRegionViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.RegionId) return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, errorMessage) = await _regionApiService.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Region '{model.RegionCode}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update region.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _regionApiService.DeactivateAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Region deactivated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to deactivate region.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _regionApiService.RestoreAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Region restored successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to restore region.";
        }

        return RedirectToAction(nameof(Index));
    }
}
