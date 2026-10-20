using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminPayrollRevisionController(PayrollDocumentsApiService api, ILogger<AdminPayrollRevisionController> logger) : PayrollDocumentsUiController(logger)
{
    // Read-only inspection of payroll versions and details for Admin
    [HttpGet]
    public Task<IActionResult> Review(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    {
        var p = await api.Payroll(id, ct);
        return View("~/Views/PayrollRevision/Review.cshtml", new PayrollReview
        {
            Payroll = p,
            History = await api.Versions(id, ct),
            PeriodStatus = await api.PeriodStatus(p.PayrollPeriodId, false, ct)
        });
    }, ct);
}
