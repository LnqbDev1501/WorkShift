using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Manager;

/// <summary>
/// Active employee types available to branch managers (e.g. staff creation dropdown).
/// </summary>
[Route("api/manager/employee-types")]
[ApiController]
[Authorize(Roles = "MANAGER")]
public sealed class ManagerEmployeeTypeController : ControllerBase
{
    private readonly IEmployeeTypeService _employeeTypeService;
    private readonly ILogger<ManagerEmployeeTypeController> _logger;

    public ManagerEmployeeTypeController(
        IEmployeeTypeService employeeTypeService,
        ILogger<ManagerEmployeeTypeController> logger)
    {
        _employeeTypeService = employeeTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all ACTIVE employee types for dropdown selection.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveEmployeeTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var paged = await _employeeTypeService.GetPagedAsync(new EmployeeTypeListQuery
            {
                Status = "ACTIVE",
                PageNumber = 1,
                PageSize = 100
            });

            var result = paged.Items
                .OrderBy(item => item.TypeName)
                .Select(item => new ActiveEmployeeTypeResponse
                {
                    EmployeeTypeId = item.EmployeeTypeId,
                    TypeName = item.TypeName
                })
                .ToList();

            return Ok(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving active employee types.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Employee types could not be retrieved. Please try again later."
            });
        }
    }
}
