using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.AttendanceRule;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class AttendanceRulesController : Controller
{
    private readonly AttendanceRuleApiService _attendanceRuleApiService;
    private readonly ILogger<AttendanceRulesController> _logger;

    public AttendanceRulesController(
        AttendanceRuleApiService attendanceRuleApiService,
        ILogger<AttendanceRulesController> logger)
    {
        _attendanceRuleApiService = attendanceRuleApiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(List));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AttendanceRuleListQueryViewModel query,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            query = new AttendanceRuleListQueryViewModel();

        try
        {
            var model = await _attendanceRuleApiService.GetListAsync(query, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to load attendance rules from API.");
            return View(new AttendanceRuleListViewModel
            {
                Query = query,
                ErrorMessage = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance rules.");
            return View(new AttendanceRuleListViewModel
            {
                Query = query,
                ErrorMessage = "Unable to load attendance rules. Please try again later."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid attendance rule ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            var model = await _attendanceRuleApiService.GetDetailAsync(id, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to load attendance rule {Id} from API.", id);
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(List));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to load attendance rule details. Please try again later.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1));
        var model = new CreateAttendanceRuleViewModel
        {
            CheckInBeforeMinutes = 30,
            CheckInGraceMinutes = 5,
            CheckOutGraceMinutes = 5,
            CheckOutAfterMinutes = 30,
            MaxAttendanceDistanceMeters = 100,
            EffectiveFrom = tomorrow
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAttendanceRuleViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var createdId = await _attendanceRuleApiService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = "Attendance rule created successfully as Draft.";
            return createdId > 0
                ? RedirectToAction(nameof(Detail), new { id = createdId })
                : RedirectToAction(nameof(List));
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create attendance rule.");
            ModelState.AddModelError(string.Empty, "Unable to create attendance rule. Please try again.");
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool returnToDetail = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _attendanceRuleApiService.PublishAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Attendance rule published successfully.";
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to publish attendance rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to publish attendance rule. Please try again.";
        }

        return returnToDetail
            ? RedirectToAction(nameof(Detail), new { id })
            : RedirectToAction(nameof(List));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancellationReason, bool returnToDetail = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _attendanceRuleApiService.CancelAsync(id, cancellationReason, cancellationToken);
            TempData["SuccessMessage"] = "Attendance rule cancelled successfully.";
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to cancel attendance rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to cancel attendance rule. Please try again.";
        }

        return returnToDetail
            ? RedirectToAction(nameof(Detail), new { id })
            : RedirectToAction(nameof(List));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _attendanceRuleApiService.DeleteAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Draft attendance rule deleted successfully.";
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete attendance rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to delete attendance rule. Please try again.";
        }

        return RedirectToAction(nameof(List));
    }
}
