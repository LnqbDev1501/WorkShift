using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Branches;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public class BranchesController : Controller
{
    private readonly BranchApiService _branchApiService;
    private readonly IVietMapService _vietMapService;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(
        BranchApiService branchApiService,
        IVietMapService vietMapService,
        ILogger<BranchesController> logger)
    {
        _branchApiService = branchApiService;
        _vietMapService = vietMapService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> SearchLocation([FromQuery] string? text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 3)
        {
            return BadRequest(new { error = "Search text must be at least 3 characters." });
        }

        var results = await _vietMapService.AutocompleteAsync(text.Trim(), cancellationToken);
        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> GetLocation([FromQuery] string? refId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refId))
        {
            return BadRequest(new { error = "refId is required." });
        }

        var place = await _vietMapService.GetPlaceAsync(refId.Trim(), cancellationToken);
        if (place == null)
        {
            return NotFound(new { error = "Location not found." });
        }

        return Ok(place);
    }

    [HttpGet]
    public async Task<IActionResult> ReverseLocation([FromQuery] double latitude, [FromQuery] double longitude, CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            return BadRequest(new { error = "Invalid latitude or longitude range." });
        }

        var result = await _vietMapService.ReverseAsync(latitude, longitude, cancellationToken);
        if (result == null)
        {
            return NotFound(new { error = "Address not found." });
        }

        return Ok(result);
    }


    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] int? regionId,
        [FromQuery] string? status = "ALL",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var viewModel = await _branchApiService.GetPagedAsync(search, regionId, status, page, pageSize, cancellationToken);
        viewModel.AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken);

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
    public Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int? regionId,
        [FromQuery] string? status = "ALL",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Index(search, regionId, status, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _branchApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Branch not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new CreateBranchViewModel
        {
            AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBranchViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _branchApiService.CreateAsync(model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Branch '{model.BranchCode.Trim().ToUpperInvariant()}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create branch.");
        model.AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var detail = await _branchApiService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Branch not found.";
            return RedirectToAction(nameof(Index));
        }

        var editModel = new EditBranchViewModel
        {
            BranchId = detail.BranchId,
            BranchCode = detail.BranchCode,
            BranchName = detail.BranchName,
            RegionId = detail.RegionId,
            Address = detail.Address,
            Latitude = detail.Latitude,
            Longitude = detail.Longitude,
            Phone = detail.Phone,
            Email = detail.Email,
            Status = detail.Status,
            EmployeeCount = detail.EmployeeCount,
            AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken)
        };

        return View(editModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditBranchViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.BranchId) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken);
            return View(model);
        }

        var (success, errorMessage) = await _branchApiService.UpdateAsync(id, model, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = $"Branch '{model.BranchCode}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update branch.");
        model.AvailableRegions = await _branchApiService.GetRegionsAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _branchApiService.DeactivateAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Branch deactivated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to deactivate branch.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest();

        var (success, errorMessage) = await _branchApiService.RestoreAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Branch restored successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to restore branch.";
        }

        return RedirectToAction(nameof(Index));
    }
}
