using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Manager;
namespace WorkShiftServer.API.Controllers.Admin;

[Route("api/admin/employee-penalties")]
public sealed class AdminEmployeePenaltiesController(IEmployeeAdjustmentService service, ILogger<AdminEmployeePenaltiesController> logger)
    : AdminEmployeeAdjustmentControllerBase(service, logger)
{ protected override string AdjustmentType => "PENALTY"; }
