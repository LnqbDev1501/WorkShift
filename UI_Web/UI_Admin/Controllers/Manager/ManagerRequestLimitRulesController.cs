using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services.Manager;
namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerRequestLimitRulesController(ManagerRequestLimitRuleApiService rules, ILogger<ManagerRequestLimitRulesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(RequestLimitRuleFilter filter, CancellationToken ct)
    {
        filter.RequestType = string.IsNullOrWhiteSpace(filter.RequestType) ? null : filter.RequestType.Trim().ToUpperInvariant();
        filter.Status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim().ToUpperInvariant();
        var model = new RequestLimitRuleListViewModel { Filter = filter };
        if (filter.RequestType is not null && !RequestRuleDisplay.Types.Contains(filter.RequestType)) ModelState.AddModelError(nameof(filter.RequestType), "Select a valid request type.");
        if (filter.Status is not null && !RequestRuleDisplay.Statuses.Contains(filter.Status)) ModelState.AddModelError(nameof(filter.Status), "Select a valid status.");
        if (!ModelState.IsValid) return View(model);
        try
        {
            model.Page = await rules.ListAsync(filter, ct);
            var lastPage = Math.Max(1, (int)Math.Ceiling(model.Page.TotalItems / (double)model.Page.PageSize));
            if (filter.PageNumber > lastPage) return RedirectToAction(nameof(Index), new { filter.RequestType, filter.Status, PageNumber = lastPage });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerRequestLimitRuleApiService.IsFailure(ex)) { logger.LogWarning(ex, "Unable to load request limit rules."); model.ErrorMessage = ManagerRequestLimitRuleApiService.Error(ex); }
        return View(model);
    }
    private async Task<RequestLimitRuleDetailViewModel> LoadAsync(int id, CancellationToken ct)
    {
        var model = new RequestLimitRuleDetailViewModel { Id = id };
        try { model.Rule = await rules.GetAsync(id, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerRequestLimitRuleApiService.IsFailure(ex)) { logger.LogWarning(ex, "Unable to load rule {RuleId}.", id); model.ErrorMessage = ManagerRequestLimitRuleApiService.Error(ex); }
        return model;
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct) => id <= 0 ? NotFound() : View(await LoadAsync(id, ct));
    [HttpGet]
    public IActionResult Create() => View("Edit", new RequestLimitRuleEditViewModel { EffectiveFrom = RequestRuleDisplay.Today, MinimumAdvanceDays = 0 });
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct) => await EditFormAsync(id, false, ct);
    [HttpGet]
    public async Task<IActionResult> Replace(int id, CancellationToken ct) => await EditFormAsync(id, true, ct);
    private async Task<IActionResult> EditFormAsync(int id, bool replace, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var detail = await LoadAsync(id, ct);
        var model = new RequestLimitRuleEditViewModel { Id = id, Mode = replace ? "Replace" : "Edit", Original = detail.Rule, ErrorMessage = detail.ErrorMessage, CanSave = false };
        if (detail.Rule is { } rule)
        {
            model.RequestType = rule.RequestType; model.MaxRequestsPerMonth = rule.MaxRequestsPerMonth;
            model.MinimumAdvanceDays = rule.MinimumAdvanceDays; model.SubmissionDeadlineDayOfWeek = rule.SubmissionDeadlineDayOfWeek;
            model.EffectiveFrom = replace ? (RequestRuleDisplay.Today > rule.EffectiveFrom ? RequestRuleDisplay.Today : rule.EffectiveFrom.AddDays(1)) : rule.EffectiveFrom;
            model.LockType = replace || rule.SupersedesRuleId.HasValue;
            model.CanSave = replace ? rule.Status == "ACTIVE" : rule.Status == "DRAFT";
            if (!model.CanSave) model.ErrorMessage = replace ? "Only active rules can be replaced." : "Only draft rules can be updated.";
        }
        return View("Edit", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind("RequestType,MaxRequestsPerMonth,MinimumAdvanceDays,SubmissionDeadlineDayOfWeek,EffectiveFrom")] RequestLimitRuleEditViewModel model, CancellationToken ct) => SaveAsync(model, null, "Create", ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind("RequestType,MaxRequestsPerMonth,MinimumAdvanceDays,SubmissionDeadlineDayOfWeek,EffectiveFrom")] RequestLimitRuleEditViewModel model, CancellationToken ct) => SaveAsync(model, id, "Edit", ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Replace(int id, [Bind("RequestType,MaxRequestsPerMonth,MinimumAdvanceDays,SubmissionDeadlineDayOfWeek,EffectiveFrom")] RequestLimitRuleEditViewModel model, CancellationToken ct) => SaveAsync(model, id, "Replace", ct);
    private async Task<IActionResult> SaveAsync(RequestLimitRuleEditViewModel model, int? id, string mode, CancellationToken ct)
    {
        if (id is <= 0) return NotFound();
        model.Id = id; model.Mode = mode;
        model.RequestType = model.RequestType?.Trim().ToUpperInvariant() ?? "";
        if (id.HasValue)
        {
            var detail = await LoadAsync(id.Value, ct); model.Original = detail.Rule;
            if (detail.Rule is not { } rule) { model.CanSave = false; model.ErrorMessage = detail.ErrorMessage; return View("Edit", model); }
            model.LockType = mode == "Replace" || rule.SupersedesRuleId.HasValue;
            if (rule.Status != (mode == "Replace" ? "ACTIVE" : "DRAFT")) { model.CanSave = false; model.ErrorMessage = mode == "Replace" ? "Only active rules can be replaced." : "Only draft rules can be updated."; return View("Edit", model); }
            if (model.LockType && model.RequestType != rule.RequestType) ModelState.AddModelError(nameof(model.RequestType), "A replacement must keep the original request type.");
            if (mode == "Replace" && model.EffectiveFrom <= rule.EffectiveFrom) ModelState.AddModelError(nameof(model.EffectiveFrom), "A replacement must start after the current version.");
        }
        if (!RequestRuleDisplay.Types.Contains(model.RequestType)) ModelState.AddModelError(nameof(model.RequestType), "Select a valid request type.");
        if (model.EffectiveFrom < RequestRuleDisplay.Today) ModelState.AddModelError(nameof(model.EffectiveFrom), "The effective date cannot be in the past.");
        if (model.SubmissionDeadlineDayOfWeek.HasValue && model.RequestType != "SALARY_INCREASE") ModelState.AddModelError(nameof(model.SubmissionDeadlineDayOfWeek), "Leave the weekday cutoff blank for this request type.");
        if (!ModelState.IsValid) return View("Edit", model);
        try
        {
            var saved = await rules.SaveAsync(model, ct);
            TempData["RuleSuccess"] = mode == "Replace" ? "Replacement draft created. Review and publish it when ready." : mode == "Edit" ? "Rule updated successfully." : "Draft rule created successfully.";
            return RedirectToAction(nameof(Details), new { id = saved.RequestLimitRuleId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerRequestLimitRuleApiService.IsFailure(ex)) { logger.LogWarning(ex, "Unable to save rule."); model.ErrorMessage = ManagerRequestLimitRuleApiService.Error(ex); return View("Edit", model); }
    }
    [HttpGet]
    public IActionResult Publish(int id) => id <= 0 ? NotFound() : RedirectToAction(nameof(Details), new { id });
    [HttpPost, ActionName("Publish"), ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishConfirmed(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = await LoadAsync(id, ct);
        if (model.Rule is null) return View("Details", model);
        if (!RequestRuleDisplay.CanPublish(model.Rule)) { model.ErrorMessage = "This draft cannot be published. Check its status, effective date, and weekday cutoff."; return View("Details", model); }
        try
        {
            var published = await rules.PublishAsync(id, ct);
            TempData["RuleSuccess"] = published.Status == "ACTIVE" ? "Rule published and now active." : "Rule published and scheduled for its effective date.";
            return RedirectToAction(nameof(Details), new { id = published.RequestLimitRuleId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerRequestLimitRuleApiService.IsFailure(ex)) { logger.LogWarning(ex, "Unable to publish rule {RuleId}.", id); model.ErrorMessage = ManagerRequestLimitRuleApiService.Error(ex); return View("Details", model); }
    }
}
