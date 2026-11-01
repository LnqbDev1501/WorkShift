using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI_Admin.Models.Manager.Requests;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
[Route("Manager/[controller]")]
public class ManagerEmployeeRequestsController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly ManagerEmployeeRequestApiService _apiService;
    private readonly ILogger<ManagerEmployeeRequestsController> _logger;

    public ManagerEmployeeRequestsController(
        ManagerEmployeeRequestApiService apiService,
        ILogger<ManagerEmployeeRequestsController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// GET /Manager/ManagerEmployeeRequests (or /Manager/Requests) — paged list of employee requests in manager's branch.
    /// </summary>
    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(
        string? search,
        string? requestType,
        string? status,
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
                    ViewBag.ErrorMessage = "Invalid date range: 'From date' must be before or on the same day as 'To date'.";
                }
            }
        }

        try
        {
            var pagedTask = _apiService.GetPagedAsync(
                search: search,
                requestType: requestType,
                status: status,
                userId: userId,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: targetPage,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            var staffTask = _apiService.GetStaffOptionsAsync(cancellationToken);

            await Task.WhenAll(pagedTask, staffTask);

            var viewModel = await pagedTask;
            viewModel.StaffOptions = await staffTask;

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
            _logger.LogError(ex, "Failed to load employee requests list for manager.");
            ViewBag.ErrorMessage = "Unable to load employee requests list. Please try again later.";

            return View(new ManagerEmployeeRequestListViewModel
            {
                PageNumber = targetPage,
                PageSize = pageSize,
                Search = search,
                RequestType = requestType,
                Status = status,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            });
        }
    }

    /// <summary>
    /// GET /Manager/ManagerEmployeeRequests/Details/{id} — detailed view of a single request.
    /// </summary>
    [HttpGet]
    [Route("Details/{id:int}")]
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
                TempData["ErrorMessage"] = "Request not found or has been deleted.";
                return RedirectToAction(nameof(Index));
            }

            // Load audit history for this specific request
            var (history, historyError) = await _apiService.GetHistoryAsync(id, cancellationToken);
            detail.History = history;
            detail.HistoryErrorMessage = historyError;

            // Safe return URL
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                detail.ReturnUrl = returnUrl;
            }

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

    /// <summary>
    /// POST /Manager/ManagerEmployeeRequests/Approve/{id} — approve request.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Approve/{id:int}")]
    public async Task<IActionResult> Approve(
        int id,
        string? reviewNote,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        try
        {
            var (isSuccess, errorMessage, statusCode) = await _apiService.ApproveAsync(id, reviewNote, cancellationToken);
            if (isSuccess)
            {
                TempData["SuccessMessage"] = $"Request #{id} has been approved successfully.";
            }
            else if (statusCode == 409)
            {
                TempData["ErrorMessage"] = "The request is no longer valid to process (status or shift has changed). Please check again.";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to approve employee request.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving employee request {RequestId}", id);
            TempData["ErrorMessage"] = $"Processing error: {ex.Message}";
        }

        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return RedirectToAction(nameof(Details), new { id, returnUrl = safeReturnUrl });
    }

    /// <summary>
    /// POST /Manager/ManagerEmployeeRequests/Reject/{id} — reject request.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Reject/{id:int}")]
    public async Task<IActionResult> Reject(
        int id,
        string? reviewNote,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        if (string.IsNullOrWhiteSpace(reviewNote))
        {
            TempData["ErrorMessage"] = "Rejection reason is required. Please provide a reason.";
            return RedirectToAction(nameof(Details), new { id, returnUrl = safeReturnUrl });
        }

        try
        {
            var (isSuccess, errorMessage, statusCode) = await _apiService.RejectAsync(id, reviewNote, cancellationToken);
            if (isSuccess)
            {
                TempData["SuccessMessage"] = $"Request #{id} has been rejected successfully.";
            }
            else if (statusCode == 409)
            {
                TempData["ErrorMessage"] = "The request is no longer valid to process (status or shift has changed). Please check again.";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to reject employee request.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting employee request {RequestId}", id);
            TempData["ErrorMessage"] = $"Processing error: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id, returnUrl = safeReturnUrl });
    }
}
