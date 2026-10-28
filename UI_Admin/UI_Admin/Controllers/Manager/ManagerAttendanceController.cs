using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager.Attendance;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public class ManagerAttendanceController : Controller
{
    private const int FixedPageSize = 20;
    private readonly ManagerAttendanceApiService _apiService;
    private readonly ILogger<ManagerAttendanceController> _logger;

    public ManagerAttendanceController(
        ManagerAttendanceApiService apiService,
        ILogger<ManagerAttendanceController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int? scheduleWeekId,
        [FromQuery] string? date,
        [FromQuery] string? search,
        [FromQuery] string? status = "ALL",
        [FromQuery] int? shiftId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageIndex = 1,
        CancellationToken cancellationToken = default)
    {
        int effectivePage = page > 1 ? page : (pageIndex > 1 ? pageIndex : 1);
        effectivePage = Math.Max(1, effectivePage);

        DateOnly? selectedDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            selectedDate = parsedDate;
        }

        var viewModel = new ManagerDailyAttendanceViewModel
        {
            SelectedScheduleWeekId = scheduleWeekId,
            Date = selectedDate,
            Search = search,
            Status = status ?? "ALL",
            ShiftId = shiftId,
            PageIndex = effectivePage,
            PageSize = FixedPageSize
        };

        try
        {
            var data = await _apiService.GetDailyAttendanceAsync(
                scheduleWeekId,
                selectedDate,
                search,
                status,
                shiftId,
                effectivePage,
                FixedPageSize,
                cancellationToken);

            if (data != null)
            {
                viewModel.BranchId = data.BranchId;
                viewModel.BranchName = data.BranchName;
                viewModel.SelectedScheduleWeekId = data.SelectedScheduleWeekId;
                viewModel.Date = data.Date;
                viewModel.TotalCount = data.TotalCount;
                viewModel.PageIndex = data.PageIndex > 0 ? data.PageIndex : effectivePage;
                viewModel.PageSize = data.PageSize > 0 ? data.PageSize : FixedPageSize;
                viewModel.Summary = data.Summary ?? new();
                viewModel.Items = data.Items ?? [];
                viewModel.AvailableShifts = data.AvailableShifts ?? [];
                viewModel.AvailableWeeks = data.AvailableWeeks ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance records.");
            viewModel.ErrorMessage = "Unable to load attendance records. Please try again later.";
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
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var detail = await _apiService.GetAttendanceDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Attendance detail not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance detail for assignment {AssignmentId}", id);
            TempData["ErrorMessage"] = "Unable to load attendance details. Please try again later.";
            return RedirectToAction(nameof(Index));
        }
    }
}
