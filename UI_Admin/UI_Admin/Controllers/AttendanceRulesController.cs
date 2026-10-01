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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
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

        return RedirectToAction(nameof(List));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancellationReason, CancellationToken cancellationToken)
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

        return RedirectToAction(nameof(List));
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
