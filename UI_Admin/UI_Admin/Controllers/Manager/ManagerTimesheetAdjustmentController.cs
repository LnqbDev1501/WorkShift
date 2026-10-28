using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager.TimesheetAdjustment;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerTimesheetAdjustmentController : Controller
{
    private readonly ManagerTimesheetAdjustmentApiService _apiService;
    private readonly ILogger<ManagerTimesheetAdjustmentController> _logger;

    public ManagerTimesheetAdjustmentController(
        ManagerTimesheetAdjustmentApiService apiService,
        ILogger<ManagerTimesheetAdjustmentController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] string? status = "ALL",
        [FromQuery] int? scheduleWeekId = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var viewModel = new ManagerTimesheetAdjustmentListViewModel
        {
            Search = search,
            Status = status ?? "ALL",
            ScheduleWeekId = scheduleWeekId,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        try
        {
            var data = await _apiService.GetAdjustmentsAsync(
                search,
                viewModel.Status,
                scheduleWeekId,
                pageIndex,
                pageSize,
                cancellationToken);

            if (data != null)
            {
                viewModel.Items = data.Items ?? new();
                viewModel.TotalCount = data.TotalCount;
                viewModel.PendingCount = data.PendingCount;
                viewModel.ApprovedCount = data.ApprovedCount;
                viewModel.RejectedCount = data.RejectedCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load timesheet adjustments list.");
            TempData["ErrorMessage"] = "Failed to load timesheet adjustment requests. Please try again.";
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid adjustment request ID.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var detail = await _apiService.GetAdjustmentDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                TempData["ErrorMessage"] = $"Adjustment request #{id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load timesheet adjustment detail #{Id}", id);
            TempData["ErrorMessage"] = "Failed to load adjustment details. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid adjustment request ID.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var (success, errorMessage) = await _apiService.ApproveAdjustmentAsync(id, cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = $"Adjustment request #{id} approved successfully. Attendance records have been updated.";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to approve adjustment request.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving adjustment request #{Id}", id);
            TempData["ErrorMessage"] = "An error occurred while approving the adjustment request.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        [FromForm] string managerNote,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid adjustment request ID.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(managerNote))
        {
            TempData["ErrorMessage"] = "Please provide a reason for rejecting the adjustment request.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var (success, errorMessage) = await _apiService.RejectAdjustmentAsync(id, managerNote.Trim(), cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = $"Adjustment request #{id} has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to reject adjustment request.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting adjustment request #{Id}", id);
            TempData["ErrorMessage"] = "An error occurred while rejecting the adjustment request.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
