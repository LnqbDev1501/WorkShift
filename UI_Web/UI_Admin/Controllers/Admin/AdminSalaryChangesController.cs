using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Controllers.SalaryChanges;
using UI_Admin.Models.SalaryChanges;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class AdminSalaryChangesController(SalaryChangeApiService api, ILogger<AdminSalaryChangesController> logger) : SalaryChangeControllerBase(api, logger)
{
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(int id, [Bind(Prefix = "Decision")] SalaryChangeDecision decision, CancellationToken ct) => Decide(id, decision, "approve", ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id, [Bind(Prefix = "Decision")] SalaryChangeDecision decision, CancellationToken ct) => Decide(id, decision, "reject", ct);
}
