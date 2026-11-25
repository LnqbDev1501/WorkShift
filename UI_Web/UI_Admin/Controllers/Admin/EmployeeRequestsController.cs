using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI_Admin.Models.EmployeeRequests;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class EmployeeRequestsController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly EmployeeRequestApiService _apiService;
    private readonly ILogger<EmployeeRequestsController> _logger;

    public EmployeeRequestsController(
        EmployeeRequestApiService apiService,
        ILogger<EmployeeRequestsController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// GET /EmployeeRequests — paged list of employee requests with search and filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? requestType,
        string? status,
        int? branchId,
        Guid? userId,
        string? fromDate,
        string? toDate,
        int page = 1,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var targetPage = Math.Max(1, page > 1 ? page : pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;

        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        // Validate date range
        if (!string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate))
        {
            if (DateOnly.TryParse(fromDate, out var from) && DateOnly.TryParse(toDate, out var to))
            {
                if (from > to)
                {
                    ViewBag.ErrorMessage = "Invalid date range: 'From Date' must be earlier than or equal to 'To Date'.";
                }
            }
        }

        try
        {
            var pagedTask = _apiService.GetPagedAsync(
                search: search,
                requestType: requestType,
                status: status,
                branchId: branchId,
                userId: userId,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: targetPage,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            var branchesTask = _apiService.GetBranchesAsync(cancellationToken);

            await Task.WhenAll(pagedTask, branchesTask);

            var viewModel = await pagedTask;
            viewModel.Branches = await branchesTask;

            if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
            {
                ViewBag.ErrorMessage = viewModel.ErrorMessage;
            }

            return View(viewModel);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load employee requests list for admin.");
            ViewBag.ErrorMessage = "Failed to load employee requests. Please try again later.";

            return View(new AdminEmployeeRequestListViewModel
            {
                PageNumber = targetPage,
                PageSize = pageSize,
                Search = search,
                RequestType = requestType,
                Status = status,
                BranchId = branchId,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            });
        }
    }

    /// <summary>
    /// GET /EmployeeRequests/Details/{id} — detailed view of a single employee request.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;

        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        try
        {
            var detail = await _apiService.GetByIdAsync(id, cancellationToken);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Employee request not found or has been removed.";
                return RedirectToAction(nameof(Index));
            }

            // Ensure returnUrl is local to prevent open redirect vulnerabilities
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                detail.ReturnUrl = returnUrl;
            }

            var (history, historyError) = await _apiService.GetHistoryAsync(id, cancellationToken);
            detail.History = history;
            detail.HistoryErrorMessage = historyError;

            return View(detail);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load details for employee request {RequestId}.", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
