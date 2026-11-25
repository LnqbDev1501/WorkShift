using Microsoft.AspNetCore.Authorization;
using UI_Admin.Controllers.EmployeeAdjustments;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT")]
public sealed class AccountantEmployeePenaltiesController(EmployeeAdjustmentApiService api,
    ILogger<AccountantEmployeePenaltiesController> logger)
    : EmployeeAdjustmentControllerBase(api, logger)
{
    protected override string AdjustmentType => EmployeeAdjustmentTypes.Penalty;
}
