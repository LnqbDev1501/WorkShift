using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[Route("api/employee-penalties")]
public sealed class EmployeePenaltiesController(IEmployeeAdjustmentService service, ILogger<EmployeePenaltiesController> logger)
    : EmployeeAdjustmentControllerBase(service, logger)
{ protected override string AdjustmentType => "PENALTY"; }
