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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid schedule week specified.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _timesheetApiService.GenerateWeeklyTimesheetAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate timesheet for week {ScheduleWeekId}", id);
            TempData["ErrorMessage"] = $"Failed to generate timesheet: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> StaffDetail(
        int scheduleWeekId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (scheduleWeekId <= 0 || userId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Invalid schedule week or staff member specified.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var weekDetail = await _timesheetApiService.GetWeeklyTimesheetDetailAsync(scheduleWeekId, cancellationToken);
            if (weekDetail == null)
            {
                TempData["ErrorMessage"] = $"Weekly timesheet details not found for week ID {scheduleWeekId}.";
                return RedirectToAction(nameof(Index));
            }

            var staffTimesheet = weekDetail.StaffTimesheets.Find(s => s.UserId == userId);
            if (staffTimesheet == null)
            {
                TempData["ErrorMessage"] = "Staff member not found in this weekly timesheet.";
                return RedirectToAction(nameof(Details), new { id = scheduleWeekId });
            }

            var viewModel = new ManagerStaffTimesheetDetailViewModel
            {
                ScheduleWeekId = weekDetail.ScheduleWeekId,
                Period = weekDetail.Period,
                WeekStartDate = weekDetail.WeekStartDate,
                WeekEndDate = weekDetail.WeekEndDate,
                BranchId = weekDetail.BranchId,
                BranchName = weekDetail.BranchName,
                WeekStatus = weekDetail.Status,
                Staff = staffTimesheet
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load staff timesheet details for week {ScheduleWeekId}, user {UserId}", scheduleWeekId, userId);
            TempData["ErrorMessage"] = "Unable to load staff timesheet details.";
            return RedirectToAction(nameof(Details), new { id = scheduleWeekId });
        }
    }
}
