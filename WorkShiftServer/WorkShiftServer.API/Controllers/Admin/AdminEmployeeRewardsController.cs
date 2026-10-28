using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Admin;

[Route("api/admin/employee-rewards")]
public sealed class AdminEmployeeRewardsController(IEmployeeAdjustmentService service, ILogger<AdminEmployeeRewardsController> logger)
    : AdminEmployeeAdjustmentControllerBase(service, logger)
{ protected override string AdjustmentType => "REWARD"; }
