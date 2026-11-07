using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ManagerPayrollCorrectionController(PayrollDocumentsApiService api, ILogger<ManagerPayrollCorrectionController> logger)
    : PayrollDocumentsUiController(logger)
{
    // Step 5: Manager inspects Correction Requests
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] int? periodId, [FromQuery] string? status, CancellationToken ct)
    {
        var requests = new List<PayrollIssueResponse>();
        try
        {
            requests = await api.AllCorrectionRequestsAsync(periodId, status, ct);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                requests = requests.Where(r =>
                    (r.EmployeeName != null && r.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Reason != null && r.Reason.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (r.PeriodCode != null && r.PeriodCode.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    r.CorrectionRequestId.ToString() == term ||
                    r.PayrollId.ToString() == term).ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        ViewBag.Search = search;
        ViewBag.PeriodId = periodId;
        ViewBag.CurrentStatus = string.IsNullOrWhiteSpace(status) ? "ALL" : status.ToUpperInvariant();
        ViewBag.PendingCount = requests.Count(r => r.Status is "OPEN" or "RETURNED");
        return View(requests);
    }

    // Step 5: Manager responds to Accountant's Correction Request (CORRECT or EXPLAIN)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(RespondCorrectionInput input, int? scheduleWeekId, int? periodId, string? returnUrl, CancellationToken ct)
    {
        if (input.CorrectionRequestId <= 0 || string.IsNullOrWhiteSpace(input.Action) || string.IsNullOrWhiteSpace(input.Note))
        {
            TempData["ErrorMessage"] = "Please select an action (EXPLAIN or CORRECT) and enter your explanation note.";
            return RedirectToTarget(returnUrl, periodId);
        }

        var normalizedAction = input.Action.Trim().ToUpperInvariant();
        if (normalizedAction is not ("CORRECT" or "EXPLAIN"))
        {
            TempData["ErrorMessage"] = "Invalid action. Supported actions are EXPLAIN or CORRECT.";
            return RedirectToTarget(returnUrl, periodId);
        }

        try
        {
            await api.RespondCorrectionAsync(input.CorrectionRequestId, normalizedAction, input.Note.Trim(), ct);
            TempData["SuccessMessage"] = $"Successfully submitted response ({normalizedAction}) to Accountant.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToTarget(returnUrl, periodId);
    }

    private IActionResult RedirectToTarget(string? returnUrl, int? periodId)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index), new { periodId });
    }
}
