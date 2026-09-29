using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.EmploymentContracts;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class EmploymentContractsController : Controller
{
    private const int DefaultPageSize = 10;

    private readonly EmploymentContractApiService _apiService;
    private readonly ILogger<EmploymentContractsController> _logger;

    public EmploymentContractsController(
        EmploymentContractApiService apiService,
        ILogger<EmploymentContractsController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// GET /EmploymentContracts — paged list with search and filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int? branchId,
        int? regionId,
        string? startDateFrom,
        string? startDateTo,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;

        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        var paged = await _apiService.GetPagedAsync(
            search, status, branchId, regionId,
            startDateFrom, startDateTo,
            pageNumber: page,
            pageSize: DefaultPageSize,
            cancellationToken: cancellationToken);

        var viewModel = new EmploymentContractListViewModel
        {
            Items = paged.Items,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            Search = search,
            StatusFilter = status,
            BranchIdFilter = branchId,
            RegionIdFilter = regionId,
            StartDateFrom = startDateFrom,
            StartDateTo = startDateTo
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET /EmploymentContracts/Details/{contractId}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0) return NotFound();

        var detail = await _apiService.GetByIdAsync(contractId, cancellationToken);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Employment contract not found.";
            return RedirectToAction(nameof(Index));
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;

        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        return View(detail);
    }
}
