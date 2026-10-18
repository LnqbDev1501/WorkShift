using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Controllers.EmployeeAdjustments;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class AdminEmployeePenaltiesController(EmployeeAdjustmentApiService api,
    ILogger<AdminEmployeePenaltiesController> logger)
    : AdminEmployeeAdjustmentReviewControllerBase(api, logger)
{
    protected override string AdjustmentType => EmployeeAdjustmentTypes.Penalty;
    protected override string EntityLabel => "Penalty";
    protected override string? DefaultApprovalStatus => "PENDING";

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(int id, [Bind(Prefix = "Decision")] ReviewEmployeeAdjustmentViewModel decision, CancellationToken ct)
        => Decide(id, decision, true, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id, [Bind(Prefix = "Decision")] ReviewEmployeeAdjustmentViewModel decision, CancellationToken ct)
        => Decide(id, decision, false, ct);
}
