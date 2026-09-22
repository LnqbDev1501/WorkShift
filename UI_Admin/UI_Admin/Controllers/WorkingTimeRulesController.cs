using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public class WorkingTimeRulesController : Controller
{
    private readonly WorkingTimeRuleApiService _workingTimeRuleApiService;
    private readonly ILogger<WorkingTimeRulesController> _logger;

    public WorkingTimeRulesController(
        WorkingTimeRuleApiService workingTimeRuleApiService,
        ILogger<WorkingTimeRulesController> logger)
    {
        _workingTimeRuleApiService = workingTimeRuleApiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(List));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            return View(await _workingTimeRuleApiService.GetListAsync(cancellationToken));
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to load working time rules.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new WorkingTimeRuleListViewModel
            {
                ErrorMessage = "Unable to load versions. Please try again later."
            });
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new WorkingTimeRuleCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        WorkingTimeRuleCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _workingTimeRuleApiService.CreateAsync(model, cancellationToken);
            TempData["WorkingTimeRuleCreatedSuccess"] = true;
            return RedirectToAction(nameof(List));
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to create working time rule.");
            ModelState.AddModelError(string.Empty,
                "Could not confirm creation. Check the version history before trying again.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        try
        {
            return View(await _workingTimeRuleApiService.GetDetailAsync(id, cancellationToken));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to load working time version {VersionId}.", id);
            TempData["ErrorMessage"] = "Unable to load this version. Please try again.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _workingTimeRuleApiService.GetDetailAsync(id, cancellationToken);
            if (!rule.CanEdit)
            {
                TempData["ErrorMessage"] = "Only Draft versions can be edited.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            return View(new WorkingTimeRuleEditViewModel
            {
                WorkingTimeRuleId = id,
                RuleName = rule.RuleName,
                MaxDailyHours = rule.MaxDailyHours,
                MaxWeeklyHours = rule.MaxWeeklyHours,
                EffectiveFrom = rule.EffectiveFrom
            });
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to load working time draft {VersionId} for editing.", id);
            TempData["ErrorMessage"] = "Unable to load this version. Please try again.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkingTimeRuleEditViewModel model, CancellationToken cancellationToken)
    {
        model.WorkingTimeRuleId = id;
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _workingTimeRuleApiService.UpdateAsync(id, model, cancellationToken);
            TempData["SuccessMessage"] = "Draft updated successfully.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to update working time draft {VersionId}.", id);
            ModelState.AddModelError(string.Empty, "Could not confirm the update. Refresh before trying again.");
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        RunActionAsync(() => _workingTimeRuleApiService.DeleteAsync(id, cancellationToken),
            "Draft deleted successfully.", cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(int id, CancellationToken cancellationToken) =>
        RunActionAsync(() => _workingTimeRuleApiService.PublishAsync(id, cancellationToken),
            "Version published. It activates on its effective date (Vietnam time).", cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
        RunActionAsync(() => _workingTimeRuleApiService.CancelAsync(id, cancellationToken),
            "Publication cancelled successfully.", cancellationToken);

    private async Task<IActionResult> RunActionAsync(Func<Task> action, string successMessage, CancellationToken cancellationToken)
    {
        try
        {
            await action();
            TempData["SuccessMessage"] = successMessage;
        }
        catch (HttpRequestException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            _logger.LogError(exception, "Unable to complete working time version action.");
            TempData["ErrorMessage"] = "Could not confirm the operation. Refresh before trying again.";
        }
        return RedirectToAction(nameof(List));
    }

    private static bool IsApiException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
