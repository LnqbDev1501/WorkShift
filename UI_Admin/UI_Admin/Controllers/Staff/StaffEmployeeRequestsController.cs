using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI_Admin.Models.Staff.Requests;
using UI_Admin.Services.Staff;

namespace UI_Admin.Controllers.Staff;

[Authorize(Roles = "STAFF")]
public class StaffEmployeeRequestsController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly StaffEmployeeRequestApiService _api;
    private readonly ILogger<StaffEmployeeRequestsController> _logger;

    public StaffEmployeeRequestsController(
        StaffEmployeeRequestApiService api,
        ILogger<StaffEmployeeRequestsController> logger)
    {
        _api = api;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? reason,
        string? requestType,
        string? status,
        string? fromDate,
        string? toDate,
        int page = 1,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
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
            var viewModel = await _api.GetPagedAsync(
                reason: reason,
                requestType: requestType,
                status: status,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: targetPage,
                pageSize: pageSize,
                cancellationToken: ct);

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
            _logger.LogError(ex, "Failed to load employee requests for staff.");
            ViewBag.ErrorMessage = "Unable to load your requests. Please try again later.";
            return View(new StaffEmployeeRequestListViewModel
            {
                PageNumber = targetPage,
                PageSize = pageSize,
                Reason = reason,
                RequestType = requestType,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl, CancellationToken ct = default)
    {
        if (id <= 0) return NotFound();

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        try
        {
            var detail = await _api.GetByIdAsync(id, ct);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Request not found or you do not have permission to view it.";
                return RedirectToAction(nameof(Index));
            }

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
            _logger.LogError(ex, "Failed to load request detail #{RequestId} for staff", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? type = "LEAVE", CancellationToken ct = default)
    {
        var normType = (type?.ToUpperInvariant() is "LATE_ARRIVAL" or "EARLY_LEAVE") ? type.ToUpperInvariant() : "LEAVE";
        ViewBag.RequestType = normType;

        var eligibleShifts = await _api.GetEligibleShiftsAsync(normType, ct);
        ViewBag.EligibleShifts = eligibleShifts;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLeave(List<int> assignmentIds, string reason, CancellationToken ct = default)
    {
        if (assignmentIds == null || !assignmentIds.Any())
        {
            TempData["ErrorMessage"] = "Please select at least one shift for leave.";
            return RedirectToAction(nameof(Create), new { type = "LEAVE" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please enter a reason for the leave request.";
            return RedirectToAction(nameof(Create), new { type = "LEAVE" });
        }

        var (success, errorMsg) = await _api.SubmitLeaveAsync(assignmentIds, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Leave request submitted successfully! Awaiting branch manager review.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Failed to submit leave request.";
        return RedirectToAction(nameof(Create), new { type = "LEAVE" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLateArrival(int assignmentId, DateTime requestedTime, string reason, CancellationToken ct = default)
    {
        if (assignmentId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a shift for late arrival.";
            return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please enter a reason for late arrival.";
            return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
        }

        var dto = new DateTimeOffset(DateTime.SpecifyKind(requestedTime, DateTimeKind.Local));
        var (success, errorMsg) = await _api.SubmitLateArrivalAsync(assignmentId, dto, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Late arrival request submitted successfully! Awaiting branch manager review.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Failed to submit late arrival request.";
        return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEarlyLeave(int assignmentId, DateTime requestedTime, string reason, CancellationToken ct = default)
    {
        if (assignmentId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a shift for early leave.";
            return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please enter a reason for early leave.";
            return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
        }

        var dto = new DateTimeOffset(DateTime.SpecifyKind(requestedTime, DateTimeKind.Local));
        var (success, errorMsg) = await _api.SubmitEarlyLeaveAsync(assignmentId, dto, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Early leave request submitted successfully! Awaiting branch manager review.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Failed to submit early leave request.";
        return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl, CancellationToken ct = default)
    {
        if (id <= 0) return NotFound();

        var (success, errorMsg) = await _api.CancelAsync(id, ct);
        if (success)
        {
            TempData["SuccessMessage"] = $"Request #{id} cancelled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMsg ?? "Failed to cancel request.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
