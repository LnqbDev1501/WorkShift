using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.EmployeeAdjustments;

[Authorize(Roles = "ADMIN")]
public abstract class AdminEmployeeAdjustmentReviewControllerBase(EmployeeAdjustmentApiService api, ILogger logger)
    : EmployeeAdjustmentControllerBase(api, logger)
{
    protected abstract string EntityLabel { get; }

    protected async Task<IActionResult> Decide(int id, ReviewEmployeeAdjustmentViewModel decision, bool approve, CancellationToken ct)
    {
        if (id <= 0) return NotFound();

        EmployeeAdjustmentItem item;
        try { item = await Api.DetailAsync(AdjustmentType, id, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }

        if (approve && !item.CanApprove) ModelState.AddModelError("", $"This {EntityLabel.ToLowerInvariant()} can no longer be approved.");
        if (!approve && !item.CanReject) ModelState.AddModelError("", $"This {EntityLabel.ToLowerInvariant()} can no longer be rejected.");
        if (!approve && string.IsNullOrWhiteSpace(decision.ReviewNote))
            ModelState.AddModelError("Decision.ReviewNote", "A rejection reason is required.");

        if (ModelState.IsValid)
        {
            try
            {
                _ = approve
                    ? await Api.ApproveAsync(AdjustmentType, id, decision, ct)
                    : await Api.RejectAsync(AdjustmentType, id, decision, ct);
                TempData["EmployeeAdjustmentSuccess"] = approve
                    ? $"{EntityLabel} approved and added to the payroll period."
                    : $"{EntityLabel} rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (EmployeeAdjustmentApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound) { return Failure(ex); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "Employee {Label} review failed for {Id}.", EntityLabel, id);
                // Reload after a conflict/timeout, since the operation may already have committed.
                TempData["EmployeeAdjustmentError"] = Error(ex);
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        return View(ViewRoot + "Details.cshtml", new EmployeeAdjustmentDetailsViewModel { Item = item, Decision = decision });
    }
}
