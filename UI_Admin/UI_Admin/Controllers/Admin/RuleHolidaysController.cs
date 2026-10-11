using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Admin.Holidays;
using UI_Admin.Services.Admin;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class RuleHolidaysController(HolidayApiService service, ILogger<RuleHolidaysController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(List));

    [HttpGet("RuleHolidays/Rules")]
    public IActionResult Rules() => RedirectToAction(nameof(List));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await service.GetRulesAsync(cancellationToken);
            return View(rules);
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to load holiday rules.");
            ViewData["LoadError"] = "Unable to load pay rules. Please try again.";
            Response.StatusCode = 503;
            return View(new List<HolidayRuleViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        try
        {
            var rules = await service.GetRulesAsync(cancellationToken);
            var rule = rules.SingleOrDefault(r => r.HolidayPayRuleId == id);
            if (rule == null) return NotFound();
            return View(rule);
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to load holiday rule {RuleId}.", id);
            TempData["ErrorMessage"] = "Unable to load pay rule details.";
            TempData["HolidayError"] = "Unable to load pay rule details.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public Task<IActionResult> Details(int id, CancellationToken cancellationToken) => Detail(id, cancellationToken);

    [HttpGet]
    public IActionResult Create()
    {
        return View(new HolidayRuleViewModel { CanEdit = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayRuleViewModel model, CancellationToken cancellationToken)
    {
        model.HolidayPayRuleId = 0;
        model.CanEdit = true;
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await service.SaveRuleAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Draft saved. Publish it when ready to apply the rule.";
                TempData["HolidaySuccess"] = "Draft saved. Publish it when ready to apply the rule.";
                return RedirectToAction(nameof(List));
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to save the pay rule.");
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to save holiday rule.");
            ModelState.AddModelError(string.Empty, "Unable to reach the server. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        try
        {
            var rules = await service.GetRulesAsync(cancellationToken);
            var rule = rules.SingleOrDefault(r => r.HolidayPayRuleId == id);
            if (rule == null) return NotFound();
            if (!rule.CanEdit)
            {
                TempData["ErrorMessage"] = "Only Draft rules can be edited. Published and active rules are read-only.";
                TempData["HolidayError"] = "Only Draft rules can be edited. Published and active rules are read-only.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            return View(rule);
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to load holiday rule {RuleId} for editing.", id);
            TempData["ErrorMessage"] = "Unable to load pay rule.";
            TempData["HolidayError"] = "Unable to load pay rule.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HolidayRuleViewModel model, CancellationToken cancellationToken)
    {
        model.HolidayPayRuleId = id;
        model.CanEdit = true;
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await service.SaveRuleAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Draft updated successfully.";
                TempData["HolidaySuccess"] = "Draft updated successfully.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to update the pay rule.");
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to update holiday rule {RuleId}.", id);
            ModelState.AddModelError(string.Empty, "Unable to reach the server. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.DeleteRuleAsync(id, cancellationToken);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Draft deleted successfully.";
                TempData["HolidaySuccess"] = "Draft deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Unable to delete holiday rule.";
                TempData["HolidayError"] = result.Error ?? "Unable to delete holiday rule.";
            }
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to delete holiday rule {RuleId}.", id);
            TempData["ErrorMessage"] = "Unable to confirm deletion. Please reload the list.";
            TempData["HolidayError"] = "Unable to confirm deletion. Please reload the list.";
        }
        return RedirectToAction(nameof(List));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteRule(int id, CancellationToken cancellationToken) => Delete(id, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.TransitionRuleAsync(id, true, cancellationToken);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Rule published. Affected payroll periods require recalculation.";
                TempData["HolidaySuccess"] = "Rule published. Affected payroll periods require recalculation.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Unable to publish holiday rule.";
                TempData["HolidayError"] = result.Error ?? "Unable to publish holiday rule.";
            }
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to publish holiday rule {RuleId}.", id);
            TempData["ErrorMessage"] = "Unable to confirm publication. Please reload the list.";
            TempData["HolidayError"] = "Unable to confirm publication. Please reload the list.";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PublishRule(int id, CancellationToken cancellationToken) => Publish(id, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.TransitionRuleAsync(id, false, cancellationToken);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Rule cancelled. Affected payroll periods require recalculation.";
                TempData["HolidaySuccess"] = "Rule cancelled. Affected payroll periods require recalculation.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Unable to cancel holiday rule.";
                TempData["HolidayError"] = result.Error ?? "Unable to cancel holiday rule.";
            }
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to cancel holiday rule {RuleId}.", id);
            TempData["ErrorMessage"] = "Unable to confirm cancellation. Please reload the list.";
            TempData["HolidayError"] = "Unable to confirm cancellation. Please reload the list.";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CancelRule(int id, CancellationToken cancellationToken) => Cancel(id, cancellationToken);

    [HttpGet]
    public IActionResult Rule(int? id)
    {
        if (id.HasValue && id.Value > 0)
            return RedirectToAction(nameof(Detail), new { id = id.Value });
        return RedirectToAction(nameof(Create));
    }

    private static bool IsApiException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException or InvalidOperationException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
