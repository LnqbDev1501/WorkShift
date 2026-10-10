using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI_Admin.Models.Manager.Timesheet;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerTimesheetController : Controller
{
    private readonly ManagerTimesheetApiService _timesheetApiService;
    private readonly ILogger<ManagerTimesheetController> _logger;

    public ManagerTimesheetController(
        ManagerTimesheetApiService timesheetApiService,
        ILogger<ManagerTimesheetController> logger)
    {
        _timesheetApiService = timesheetApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] int? branchId,
        [FromQuery] int? scheduleWeekId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] string? status = "ALL",
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var viewModel = new ManagerTimesheetListViewModel
        {
            Search = search,
            BranchId = branchId,
            ScheduleWeekId = scheduleWeekId,
            Month = month,
            Year = year ?? DateTime.UtcNow.AddHours(7).Year,
            Status = status ?? "ALL",
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        try
        {
            var pagedData = await _timesheetApiService.GetWeeklyTimesheetsAsync(
                search,
                branchId,
                scheduleWeekId,
                month,
                viewModel.Year,
                status,
                pageIndex,
                pageSize,
                cancellationToken);

            viewModel.Items = pagedData.Items;
            viewModel.AvailablePeriods = pagedData.AvailablePeriods;
            viewModel.TotalItems = pagedData.TotalItems;
            viewModel.TotalPages = pagedData.TotalPages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load manager weekly timesheets.");
            viewModel.ErrorMessage = "Unable to load timesheet list. Please try again.";
        }

        if (TempData["SuccessMessage"] is string success)
            viewModel.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            viewModel.ErrorMessage = error;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid schedule week specified.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var detail = await _timesheetApiService.GetWeeklyTimesheetDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                TempData["ErrorMessage"] = $"Weekly timesheet details not found for week ID {id}.";
                return RedirectToAction(nameof(Index));
            }

            if (TempData["SuccessMessage"] is string success)
                detail.SuccessMessage = success;
            if (TempData["ErrorMessage"] is string error)
                detail.ErrorMessage = error;

            return View(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load manager weekly timesheet details for week {ScheduleWeekId}", id);
            TempData["ErrorMessage"] = "Unable to load timesheet details. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}
