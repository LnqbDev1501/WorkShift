using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminPayrollCalculationController(PayrollCalculationApiService api, ILogger<AdminPayrollCalculationController> logger) : PayrollCalculationUiController(api, logger)
{
    [HttpGet]
    public async Task<IActionResult> Index(int id, [FromQuery] CalculationQuery query, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = Model(id, query, false);
        if (ModelState.IsValid)
            try { await Load(model, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.Error = Error(ex); }
        return View(ListView, model);
    }
    [HttpGet] public IActionResult Details(int id) => id <= 0 ? NotFound() : RedirectToAction("Review", "AdminPayrollRevision", new { id });
}
