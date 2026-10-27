using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;
namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollCalculationController(PayrollCalculationApiService api, PayrollCalculationActionApiService actions, ILogger<AccountantPayrollCalculationController> logger, PayrollPeriodApiService periods) : PayrollCalculationUiController(api, logger)
{
    [HttpGet]
    public async Task<IActionResult> Periods([FromQuery] UI_Admin.Models.Accountant.PayrollPeriods.PeriodQuery query, CancellationToken ct)
    {
        var model = new UI_Admin.Models.Accountant.PayrollPeriods.PeriodListViewModel { Query = query };
        query.Status = query.Status?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(query.Status) && !UI_Admin.Models.Accountant.PayrollPeriods.PeriodDisplay.Statuses.Contains(query.Status)) ModelState.AddModelError("Status", "Trạng thái kỳ không hợp lệ.");
        if (query.StartDateFrom > query.StartDateTo) ModelState.AddModelError("StartDateTo", "Khoảng ngày không hợp lệ.");
        if (ModelState.IsValid)
        {
            try { model.Page = await periods.ListAsync(query, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (PayrollPeriodApiException ex)
            {
                if (ex.Status == System.Net.HttpStatusCode.Unauthorized) return Challenge();
                if (ex.Status == System.Net.HttpStatusCode.Forbidden) return Forbid();
                model.Error = ex.Message;
            }
            catch (Exception ex) when (Handled(ex)) { model.Error = Error(ex); }
        }
        return View("~/Views/PayrollCalculation/Periods.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id, Guid? userId, CancellationToken ct)
    {
        if (id <= 0 || userId == Guid.Empty || !ModelState.IsValid) return BadRequest();
        var model = new CalculationPreviewViewModel { PeriodId = id, UserId = userId };
        try
        {
            if (userId.HasValue)
            {
                model.Inputs = await actions.InputsAsync(id, userId.Value, ct);
                if (model.Inputs.Readiness.CanCalculate) model.Payrolls.Add(await actions.PreviewEmployeeAsync(id, userId.Value, ct));
                else model.FailedEmployees.Add(model.Inputs.Readiness);
            }
            else
            {
                var batch = await actions.PreviewPeriodAsync(id, ct);
                model.Payrolls = batch.Payrolls; model.FailedEmployees = batch.FailedEmployees;
            }
            if (model.CanSave)
            {
                // A protected TempData ticket ties the save button to the previewed employee/period.
                model.ReviewToken = Guid.NewGuid().ToString("N");
                TempData["CalculationPreview:" + model.ReviewToken] = $"{id}:{userId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.Error = Error(ex); }
        return View("~/Views/PayrollCalculation/Preview.cshtml", model);
    }

    private bool Reviewed(int id, Guid? userId, string? reviewToken)
    {
        if (!Guid.TryParseExact(reviewToken, "N", out _)) return false;
        var value = TempData["CalculationPreview:" + reviewToken] as string;
        TempData.Remove("CalculationPreview:" + reviewToken);
        var parts = value?.Split(':');
        return parts?.Length == 3 && parts[0] == id.ToString() && parts[1] == (userId?.ToString() ?? "")
            && long.TryParse(parts[2], out var time) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - time is >= 0 and <= 900;
    }

    private async Task<IActionResult> Screen(CalculationViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                model.Readiness = await actions.ReadinessAsync(model.PeriodId, ct);
                var employees = model.Readiness.Employees.AsEnumerable();
                var keyword = model.Query.Keyword?.Trim();
                if (!string.IsNullOrEmpty(keyword)) employees = employees.Where(e => e.EmployeeName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || e.UserId.ToString().Equals(keyword, StringComparison.OrdinalIgnoreCase));
                if (model.Query.UserId.HasValue) employees = employees.Where(e => e.UserId == model.Query.UserId);
                if (!string.IsNullOrEmpty(model.Query.Status)) employees = employees.Where(e => model.Query.Status == "NOT_CALCULATED" ? e.ExistingPayrollId == null : e.ExistingPayrollStatus == model.Query.Status);
                if (model.Query.CalculationState == "READY") employees = employees.Where(e => e.CanCalculate);
                if (model.Query.CalculationState == "BLOCKED") employees = employees.Where(e => !e.CanCalculate && e.ExistingPayrollId == null);
                var filtered = employees.ToList(); model.ReadinessTotal = filtered.Count;
                model.Query.Page = Math.Min(model.Query.Page, Math.Max(1, (int)Math.Ceiling(filtered.Count / 20d)));
                model.Readiness.Employees = filtered.Skip((model.Query.Page - 1) * 20).Take(20).ToList();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.ReadinessError = Error(ex); }

        }
        // POST outcomes reuse the list view; its pagination must target the GET Index action.
        RouteData.Values["action"] = nameof(Index);
        return View(ListView, model);
    }
    [HttpGet]
    public Task<IActionResult> Index(int id, [FromQuery] CalculationQuery query, CancellationToken ct)
    {
        if (id <= 0) return Task.FromResult<IActionResult>(NotFound());
        // NOT_CALCULATED is a readiness filter, not a persisted Payroll status.
        var status = query.Status?.Trim().ToUpperInvariant();
        if (status == "NOT_CALCULATED") query.Status = null;
        var model = Model(id, query, true);
        if (status == "NOT_CALCULATED") model.Query.Status = status;
        query.CalculationState = query.CalculationState?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(query.CalculationState) && query.CalculationState is not ("READY" or "BLOCKED")) ModelState.AddModelError("CalculationState", "Điều kiện tính không hợp lệ.");
        return Screen(model, ct);
    }
    [HttpGet] public Task<IActionResult> Details(int id, CancellationToken ct) => ReadDetail(id, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculatePeriod(int id, string? reviewToken, CancellationToken ct)
    {
        if (id <= 0 || !ModelState.IsValid) return BadRequest();
        if (!Reviewed(id, null, reviewToken)) return RedirectToAction(nameof(Preview), new { id });
        var model = Model(id, new(), true);
        try
        {
            model.Batch = await actions.PeriodAsync(id, ct);
            TempData["WorkflowSuccess"] = "Tính lương toàn kỳ thành công! Tất cả nhân viên đã có Payroll V1 (CALCULATED).";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            model.Error = Error(ex);
        }
        return await Screen(model, ct);
    }
}
