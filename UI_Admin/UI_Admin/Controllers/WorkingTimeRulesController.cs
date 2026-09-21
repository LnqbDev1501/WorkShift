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

    private static bool IsApiException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
