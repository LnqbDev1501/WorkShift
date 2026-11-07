using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Controllers.EmployeeAdjustments;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class AdminEmployeeRewardsController(EmployeeAdjustmentApiService api,
    ILogger<AdminEmployeeRewardsController> logger)
    : AdminEmployeeAdjustmentReviewControllerBase(api, logger)
{
    protected override string AdjustmentType => EmployeeAdjustmentTypes.Reward;
    protected override string EntityLabel => "Reward";

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(int id, [Bind(Prefix = "Decision")] ReviewEmployeeAdjustmentViewModel decision, CancellationToken ct)
        => Decide(id, decision, true, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id, [Bind(Prefix = "Decision")] ReviewEmployeeAdjustmentViewModel decision, CancellationToken ct)
        => Decide(id, decision, false, ct);
}
