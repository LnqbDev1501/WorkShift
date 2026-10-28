using Microsoft.AspNetCore.Authorization;
using UI_Admin.Controllers.EmployeeAdjustments;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerEmployeeRewardsController(EmployeeAdjustmentApiService api,
    ManagerStaffApiService staff, ILogger<ManagerEmployeeRewardsController> logger)
    : ManagerEmployeeAdjustmentControllerBase(api, staff, logger)
{
    protected override string AdjustmentType => EmployeeAdjustmentTypes.Reward;
    protected override string EntityLabel => "Reward";
}
