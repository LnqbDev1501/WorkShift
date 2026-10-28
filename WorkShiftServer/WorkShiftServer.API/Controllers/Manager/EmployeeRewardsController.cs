using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[Route("api/employee-rewards")]
public sealed class EmployeeRewardsController(IEmployeeAdjustmentService service, ILogger<EmployeeRewardsController> logger)
    : EmployeeAdjustmentControllerBase(service, logger)
{ protected override string AdjustmentType => "REWARD"; }
