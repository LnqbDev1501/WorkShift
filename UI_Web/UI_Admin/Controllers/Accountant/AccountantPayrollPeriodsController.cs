using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant.PayrollPeriods;
using UI_Admin.Models.Accountant.PayrollSettlement;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollPeriodsController(
    PayrollPeriodApiService api,
    PayrollDocumentsApiService docs,
    PayrollPeriodReportApiService reports,
    ILogger<AccountantPayrollPeriodsController> logger) : Controller
{
    private IActionResult? Access(Exception ex)
    {
        var status = ex is PayrollPeriodApiException p ? p.Status : (ex as HttpRequestException)?.StatusCode;
        return status switch
        {
            HttpStatusCode.Unauthorized => Challenge(),
            HttpStatusCode.Forbidden => Forbid(),
            HttpStatusCode.NotFound => NotFound(),
            _ => null
        };
    }

    private static bool Handled(Exception ex) => ex is PayrollPeriodApiException or HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException;

    private string Error(Exception ex)
    {
        logger.LogWarning(ex, "Payroll period UI request failed.");
        return ex is PayrollPeriodApiException || ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict }
            ? ex.Message : "Unable to confirm operation result. Please refresh the payroll period to verify before retrying.";
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PeriodQuery query, CancellationToken ct)
    {
        query.Status = query.Status?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(query.Status) && !PeriodDisplay.Statuses.Contains(query.Status))
            ModelState.AddModelError("Status", "Invalid status.");
        if (query.StartDateFrom > query.StartDateTo)
            ModelState.AddModelError("StartDateTo", "Filter end date must be on or after start date.");

        var model = new PeriodListViewModel { Query = query };
        if (ModelState.IsValid)
        {
            try
            {
                model.Page = await api.ListAsync(query, ct);
                if (query.PageNumber > model.Page.TotalPages)
                {
                    query.PageNumber = model.Page.TotalPages;
                    model.Page = await api.ListAsync(query, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex))
            {
                if (Access(ex) is IActionResult denied) return denied;
                model.Error = Error(ex);
            }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var period = await api.DetailAsync(id, ct);
            try
            {
                period.CorrectionRequests = await api.CorrectionRequestsAsync(id, ct);
            }
            catch { /* non-blocking optional fetch */ }

            try
            {
                var feedbackPage = await docs.Get<PayrollFeedbackListResponse>($"/payroll-previews?payrollPeriodId={id}&pageSize=100", ct);
                period.Feedbacks = feedbackPage.Items;
            }
            catch { /* non-blocking optional fetch */ }

            if (period.Status is "APPROVED" or "PAID" or "CLOSED")
            {
                try
                {
                    period.SettlementSummary = await api.SettlementSummaryAsync(id, ct);
                }
                catch { /* non-blocking optional fetch */ }
            }

            return View(period);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task LoadWeeks(PeriodForm form, PeriodRecord? current, CancellationToken ct)
    {
        form.Weeks = await api.WeeksAsync(ct);
        if (current?.ScheduleWeekId is int week && !form.Weeks.Any(w => w.ScheduleWeekId == week))
        {
            form.Weeks.Insert(0, new()
            {
                ScheduleWeekId = week,
                BranchId = current.BranchId,
                WeekStartDate = current.StartDate,
                WeekEndDate = current.EndDate,
                Status = "Current week"
            });
        }
        form.WeeksLoaded = true;
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var form = new PeriodForm();
        try { await LoadWeeks(form, null, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            ModelState.AddModelError("", Error(ex));
        }
        return View("Form", form);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var current = await api.DetailAsync(id, ct);
            if (!current.CanUpdate)
            {
                TempData["PeriodError"] = "This draft period can no longer be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }
            var form = new PeriodForm { Id = id, PeriodName = current.PeriodName, ScheduleWeekId = current.ScheduleWeekId ?? 0 };
            await LoadWeeks(form, current, ct);
            return View("Form", form);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind("PeriodName,ScheduleWeekId")] PeriodForm form, CancellationToken ct) => Save(form, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind("Id,PeriodName,ScheduleWeekId")] PeriodForm form, CancellationToken ct)
    {
        if (id <= 0 || id != form.Id) return Task.FromResult<IActionResult>(BadRequest());
        return Save(form, ct);
    }

    private async Task<IActionResult> Save(PeriodForm form, CancellationToken ct)
    {
        PeriodRecord? current = null;
        try
        {
            if (form.Id != 0)
            {
                current = await api.DetailAsync(form.Id, ct);
                if (!current.CanUpdate)
                {
                    TempData["PeriodError"] = "Draft period can no longer be edited.";
                    return RedirectToAction(nameof(Details), new { id = form.Id });
                }
            }
            if (ModelState.IsValid)
            {
                var id = await api.SaveAsync(form, ct);
                TempData["PeriodSuccess"] = form.Id == 0 ? "Draft payroll period created successfully." : "Draft period updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            ModelState.AddModelError("", Error(ex));
        }
        try { await LoadWeeks(form, current, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            ModelState.AddModelError("", Error(ex));
        }
        return View("Form", form);
    }

    [HttpGet] public Task<IActionResult> Open(int id, CancellationToken ct) => Confirm(id, false, ct);
    [HttpGet] public Task<IActionResult> Delete(int id, CancellationToken ct) => Confirm(id, true, ct);

    private async Task<IActionResult> Confirm(int id, bool delete, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var p = await api.DetailAsync(id, ct);
            if (!(delete ? p.CanDelete : p.CanOpen))
            {
                TempData["PeriodError"] = "Payroll period is no longer eligible for this operation.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View("Confirm", new PeriodConfirmation(p, delete));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Open"), ValidateAntiForgeryToken] public Task<IActionResult> OpenConfirmed(int id, CancellationToken ct) => Mutate(id, false, ct);
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken] public Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct) => Mutate(id, true, ct);

    private async Task<IActionResult> Mutate(int id, bool delete, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var p = await api.DetailAsync(id, ct);
            if (!(delete ? p.CanDelete : p.CanOpen))
                throw new PayrollPeriodApiException(HttpStatusCode.Conflict, "Period status has changed; operation is no longer permitted.");
            if (delete) await api.DeleteAsync(id, ct);
            else await api.OpenAsync(id, ct);
            TempData["PeriodSuccess"] = delete ? "Draft period deleted successfully." : "Payroll period opened successfully (OPEN). Ready for calculation.";
            return delete ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Send Previews to Staff
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPreviews(int id, DateTime? initialFeedbackDeadlineAt, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            DateTimeOffset? deadlineUtc = null;
            if (initialFeedbackDeadlineAt.HasValue)
            {
                // Browser datetime-local submits local Vietnam time (UTC+7)
                var vnTime = initialFeedbackDeadlineAt.Value;
                var offset = new DateTimeOffset(vnTime, TimeSpan.FromHours(7));
                deadlineUtc = offset.ToUniversalTime();
            }
            await api.SendPreviewsAsync(id, deadlineUtc, ct);
            TempData["PeriodSuccess"] = "Payroll previews have been sent to all eligible employees. Review deadline has been set.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Submit Period to Admin
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPeriod(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            await api.SubmitPeriodAsync(id, ct);
            TempData["PeriodSuccess"] = "Payroll period submitted to Admin for approval successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // View Settlement Summary
    [HttpGet]
    public async Task<IActionResult> Settlement(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var summary = await api.SettlementSummaryAsync(id, ct);
            return View(summary);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Confirm Settlement
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSettlement(int id, ConfirmPayrollSettlementRequest request, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        if (!request.ConfirmedExternalSettlement)
        {
            TempData["PeriodError"] = "Please confirm that all external net salary payments have been completed.";
            return RedirectToAction(nameof(Settlement), new { id });
        }
        try
        {
            await api.ConfirmSettlementAsync(id, request, ct);
            TempData["PeriodSuccess"] = "Settlement confirmed successfully. Applied Carry Forward deductions and closed the period (PAID -> CLOSED).";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Settlement), new { id });
        }
    }

    // Source Impact Check
    [HttpGet]
    public async Task<IActionResult> SourceImpact(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var impact = await api.SourceImpactAsync(id, ct);
            return View(impact);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var (fileBytes, fileName, contentType) = await reports.DownloadAsync(id, ct);
            return File(fileBytes, contentType, fileName);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["PeriodError"] = Error(ex);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public Task<IActionResult> ExportExcel(int id, CancellationToken ct) => Export(id, ct);
}