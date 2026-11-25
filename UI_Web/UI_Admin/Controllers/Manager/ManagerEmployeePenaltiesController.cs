using Microsoft.AspNetCore.Authorization;
using UI_Admin.Controllers.EmployeeAdjustments;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerEmployeePenaltiesController(EmployeeAdjustmentApiService api,
    ManagerStaffApiService staff, ILogger<ManagerEmployeePenaltiesController> logger)
    : ManagerEmployeeAdjustmentControllerBase(api, staff, logger)
{
    protected override string AdjustmentType => EmployeeAdjustmentTypes.Penalty;
    protected override string EntityLabel => "Penalty";
}
