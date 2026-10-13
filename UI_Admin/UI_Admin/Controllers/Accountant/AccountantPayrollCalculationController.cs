using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;
namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollCalculationController(PayrollCalculationApiService api, PayrollCalculationActionApiService actions, ILogger<AccountantPayrollCalculationController> logger) : PayrollCalculationUiController(api, logger)
{
    private async Task<IActionResult> Screen(CalculationViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try { await Load(model, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.Error ??= Error(ex); }
            try { model.Readiness = await actions.ReadinessAsync(model.PeriodId, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.ReadinessError = Error(ex); }
        }
        // POST outcomes reuse the list view; its pagination must target the GET Index action.
        RouteData.Values["action"] = nameof(Index);
        return View(ListView, model);
    }
    [HttpGet]
    public Task<IActionResult> Index(int id, [FromQuery] CalculationQuery query, CancellationToken ct)
        => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Screen(Model(id, query, true), ct);
    [HttpGet] public Task<IActionResult> Details(int id, CancellationToken ct) => ReadDetail(id, ct);
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateEmployee(int id, Guid userId, CancellationToken ct)
    {
        if (id <= 0 || userId == Guid.Empty || !ModelState.IsValid) return BadRequest();
        try { var result = await actions.EmployeeAsync(id, userId, ct); return RedirectToAction(nameof(Details), new { id = result.PayrollId }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            var model = Model(id, new(), true); model.Error = Error(ex); model.BlockedEmployee = (ex as CalculationApiException)?.Readiness;
            return await Screen(model, ct);
        }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculatePeriod(int id, CancellationToken ct)
    {
        if (id <= 0 || !ModelState.IsValid) return BadRequest();
        var model = Model(id, new(), true);
        try { model.Batch = await actions.PeriodAsync(id, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.Error = Error(ex); }
        return await Screen(model, ct);
    }
}
