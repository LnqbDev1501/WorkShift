using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.SalaryChanges;
using UI_Admin.Services;

namespace UI_Admin.Controllers.SalaryChanges;

public abstract class SalaryChangeControllerBase(SalaryChangeApiService api, ILogger logger) : Controller
{
    protected readonly SalaryChangeApiService Api = api;
    protected const string ViewRoot = "~/Views/Shared/SalaryChanges/";
    protected IActionResult Failure(Exception ex)
    {
        if (ex is SalaryChangeApiException { Status: HttpStatusCode.Unauthorized }) return RedirectToAction("Login", "Auth");
        if (ex is SalaryChangeApiException { Status: HttpStatusCode.Forbidden }) return Forbid();
        if (ex is SalaryChangeApiException { Status: HttpStatusCode.NotFound }) return NotFound();
        logger.LogError(ex, "Salary change UI request failed.");
        TempData["SalaryChangeError"] = Error(ex);
        return RedirectToAction(nameof(Index));
    }
    protected static string Error(Exception ex) => ex is SalaryChangeApiException ? ex.Message : "Unable to connect to the server. Reload the request before trying again.";
    [HttpGet]
    public async Task<IActionResult> Index(SalaryChangeQuery query, CancellationToken ct)
    {
        if (!User.IsInRole("ADMIN")) { query.BranchId = null; ModelState.Remove(nameof(query.BranchId)); }
        var model = new SalaryChangeListViewModel { Query = query };
        if (!ModelState.IsValid) return View(ViewRoot + "Index.cshtml", model);
        try { model = await Api.ListAsync(query, ct); }
        catch (SalaryChangeApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { return Failure(ex); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Salary change list failed."); model.ErrorMessage = Error(ex); }
        return View(ViewRoot + "Index.cshtml", model);
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return View(ViewRoot + "Details.cshtml", new SalaryChangeDetailsViewModel { Item = await Api.DetailAsync(id, ct) }); }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
    }
    protected async Task<IActionResult> Decide(int id, SalaryChangeDecision decision, string action, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        SalaryChangeItem item;
        try { item = await Api.DetailAsync(id, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
        if (action == "approve")
        {
            if (!item.CanApprove) ModelState.AddModelError("", item.ApprovalBlockedReason ?? "This request cannot be approved.");
            if (item.RequiresEffectiveDateChange)
            {
                if (!decision.ApprovedEffectiveFrom.HasValue || decision.ApprovedEffectiveFrom < item.EarliestAllowedEffectiveFrom || decision.ApprovedEffectiveFrom.Value.DayOfWeek != DayOfWeek.Monday)
                    ModelState.AddModelError("Decision.ApprovedEffectiveFrom", "Select an eligible Monday from the next working week onward.");
                if (string.IsNullOrWhiteSpace(decision.EffectiveDateChangeReason)) ModelState.AddModelError("Decision.EffectiveDateChangeReason", "Please explain the date change.");
            }
            else { decision.ApprovedEffectiveFrom = null; decision.EffectiveDateChangeReason = null; }
        }
        if (action == "reject" && (!item.CanReject || string.IsNullOrWhiteSpace(decision.ReviewNote))) ModelState.AddModelError("Decision.ReviewNote", "A pending request and rejection reason are required.");
        if (action == "cancel" && (!item.CanCancel || string.IsNullOrWhiteSpace(decision.Reason))) ModelState.AddModelError("Decision.Reason", "Only your pending request can be cancelled; a reason is required.");
        if (ModelState.IsValid)
        {
            try
            {
                _ = action switch { "approve" => await Api.ApproveAsync(id, decision, ct), "reject" => await Api.RejectAsync(id, decision, ct), _ => await Api.CancelAsync(id, decision, ct) };
                TempData["SalaryChangeSuccess"] = action switch { "approve" => "Request approved. The new rate is scheduled for its effective date.", "reject" => "Request rejected.", _ => "Request cancelled." };
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (SalaryChangeApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound) { return Failure(ex); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Salary decision failed for {Id}.", id);
                // Reload after a conflict/timeout, since the operation may already have committed.
                TempData["SalaryChangeError"] = Error(ex);
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        return View(ViewRoot + "Details.cshtml", new SalaryChangeDetailsViewModel { Item = item, Decision = decision });
    }
}
