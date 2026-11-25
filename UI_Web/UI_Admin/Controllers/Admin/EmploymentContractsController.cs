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
    public async Task<IActionResult> Details(
        int contractId,
        int historyPage = 1,
        int historyPageSize = 10,
        CancellationToken cancellationToken = default)
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

        historyPage = Math.Max(1, historyPage);
        historyPageSize = Math.Clamp(historyPageSize, 1, 50);

        try
        {
            detail.History = await _apiService.GetHistoryAsync(
                contractId,
                pageNumber: historyPage,
                pageSize: historyPageSize,
                sort: "desc",
                cancellationToken: cancellationToken);

            if (detail.History == null)
            {
                detail.HistoryLoadFailed = true;
                detail.HistoryErrorMessage = "Unable to load history from the server.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load history for contract {ContractId}", contractId);
            detail.HistoryLoadFailed = true;
            detail.HistoryErrorMessage = "Unable to connect to the history service.";
        }

        return View(detail);
    }

    /// <summary>
    /// GET /EmploymentContracts/Pdf/{id} — inline PDF viewer
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var (bytes, contentType, fileName) = await _apiService.GetPdfFileAsync(id, isDownload: false, cancellationToken);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, contentType);
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { contractId = id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to open the contract PDF.";
            return RedirectToAction(nameof(Details), new { contractId = id });
        }
    }

    /// <summary>
    /// GET /EmploymentContracts/DownloadPdf/{id} — download PDF
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var (bytes, contentType, fileName) = await _apiService.GetPdfFileAsync(id, isDownload: true, cancellationToken);
            return File(bytes, contentType, fileName);
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { contractId = id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to download the contract PDF.";
            return RedirectToAction(nameof(Details), new { contractId = id });
        }
    }
}
