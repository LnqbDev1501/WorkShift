using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Admin.AttendancePenaltyRule;
using UI_Admin.Services.Admin;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public class AttendancePenaltyRulesController : Controller
{
    private readonly AttendancePenaltyRuleApiService _penaltyRuleApiService;
    private readonly ILogger<AttendancePenaltyRulesController> _logger;

    public AttendancePenaltyRulesController(
        AttendancePenaltyRuleApiService penaltyRuleApiService,
        ILogger<AttendancePenaltyRulesController> logger)
    {
        _penaltyRuleApiService = penaltyRuleApiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(string? searchTerm, string? status, int pageNumber = 1)
    {
        return RedirectToAction(nameof(List), new { searchTerm, status, pageNumber });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        string? searchTerm,
        string? status,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _penaltyRuleApiService.GetListAsync(searchTerm, status, pageNumber, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to load attendance penalty rules from API.");
            TempData["ErrorMessage"] = exception.Message;
            return View(new AttendancePenaltyRuleListViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                PageNumber = pageNumber
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance penalty rules.");
            TempData["ErrorMessage"] = "Unable to load attendance penalty rules. Please try again later.";
            return View(new AttendancePenaltyRuleListViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                PageNumber = pageNumber
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid rule ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            var model = await _penaltyRuleApiService.GetDetailAsync(id, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to load attendance penalty rule {Id} from API.", id);
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(List));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance penalty rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to load penalty rule details. Please try again later.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1));
        var model = new CreateAttendancePenaltyRuleViewModel
        {
            EffectiveFrom = tomorrow,
            PenaltyLevels = new List<CreateAttendancePenaltyLevelViewModel>
            {
                // Only 1 default row for Late and 1 default row for Early Leave
                new() { ViolationType = "LATE", FromMinutes = 1, ToMinutes = 15, PenaltyAmount = 20000 },
                new() { ViolationType = "EARLY_LEAVE", FromMinutes = 1, ToMinutes = 15, PenaltyAmount = 20000 }
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAttendancePenaltyRuleViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var createdId = await _penaltyRuleApiService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Draft penalty rule '{model.RuleName}' created successfully!";
            return RedirectToAction(nameof(Detail), new { id = createdId });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to create attendance penalty rule via API.");
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating attendance penalty rule.");
            ModelState.AddModelError(string.Empty, "Unable to create attendance penalty rule. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid rule ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            var rule = await _penaltyRuleApiService.GetDetailAsync(id, cancellationToken);
            if (!rule.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only rules in 'Draft' status can be edited.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var model = new CreateAttendancePenaltyRuleViewModel
            {
                RuleName = rule.RuleName,
                Description = rule.Description,
                EffectiveFrom = rule.EffectiveFrom,
                PenaltyLevels = rule.PenaltyLevels.Select(l => new CreateAttendancePenaltyLevelViewModel
                {
                    ViolationType = l.ViolationType,
                    FromMinutes = l.FromMinutes,
                    ToMinutes = l.ToMinutes,
                    PenaltyAmount = l.PenaltyAmount
                }).ToList()
            };

            ViewBag.PenaltyRuleId = id;
            return View(model);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to load penalty rule {Id} for edit.", id);
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(List));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while loading penalty rule {Id} for edit.", id);
            TempData["ErrorMessage"] = "Unable to load penalty rule for editing.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CreateAttendancePenaltyRuleViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PenaltyRuleId = id;
            return View(model);
        }

        try
        {
            await _penaltyRuleApiService.UpdateAsync(id, model, cancellationToken);
            TempData["SuccessMessage"] = $"Penalty rule '{model.RuleName}' updated successfully!";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to update attendance penalty rule {Id} via API.", id);
            ModelState.AddModelError(string.Empty, exception.Message);
            ViewBag.PenaltyRuleId = id;
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating attendance penalty rule {Id}.", id);
            ModelState.AddModelError(string.Empty, "Unable to update attendance penalty rule. Please try again.");
            ViewBag.PenaltyRuleId = id;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, string? source, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid rule ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            await _penaltyRuleApiService.PublishAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Attendance penalty rule published successfully!";
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to publish attendance penalty rule {Id} via API.", id);
            TempData["ErrorMessage"] = exception.Message;
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while publishing attendance penalty rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to publish penalty rule. Please try again.";
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        string? cancellationReason,
        string? source,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] = "Invalid rule ID.";
            return RedirectToAction(nameof(List));
        }

        try
        {
            await _penaltyRuleApiService.CancelAsync(id, cancellationReason, cancellationToken);
            TempData["SuccessMessage"] = "Attendance penalty rule cancelled successfully.";
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to cancel attendance penalty rule {Id} via API.", id);
            TempData["ErrorMessage"] = exception.Message;
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while cancelling attendance penalty rule {Id}.", id);
            TempData["ErrorMessage"] = "Unable to cancel penalty rule. Please try again.";
            
            if (string.Equals(source, "list", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(List));
            }
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
