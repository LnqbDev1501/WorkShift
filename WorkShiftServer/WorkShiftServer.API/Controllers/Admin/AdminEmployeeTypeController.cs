using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Route("api/admin/employee-types")]
[Authorize(Policy = "Admin")]
public class AdminEmployeeTypeController : ControllerBase
{
    private readonly IEmployeeTypeService _employeeTypeService;

    public AdminEmployeeTypeController(IEmployeeTypeService employeeTypeService)
    {
        _employeeTypeService = employeeTypeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeTypeResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetPaged([FromQuery] EmployeeTypeListQuery query) =>
        ExecuteAsync(async () => Ok(await _employeeTypeService.GetPagedAsync(query)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _employeeTypeService.GetByIdAsync(id);
        return result == null ? Missing() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeTypeResponse), StatusCodes.Status201Created)]
    public Task<IActionResult> Create([FromBody] CreateEmployeeTypeRequest request) => ExecuteAsync(async () =>
    {
        var result = await _employeeTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.EmployeeTypeId }, result);
    });

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeTypeRequest request) => ExecuteAsync(async () =>
    {
        var result = await _employeeTypeService.UpdateAsync(id, request);
        return result == null ? Missing() : Ok(result);
    });

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id) => ExecuteAsync(async () =>
        StatusResult(await _employeeTypeService.DeleteAsync(id), restore: false));

    [HttpPost("{id:int}/restore")]
    public Task<IActionResult> Restore(int id) => ExecuteAsync(async () =>
        StatusResult(await _employeeTypeService.RestoreAsync(id), restore: true));

    private IActionResult StatusResult(EmployeeTypeStatusChangeResult result, bool restore)
    {
        if (result == EmployeeTypeStatusChangeResult.Changed)
            return Ok(new
            {
                result = result.ToString(),
                message = restore ? "Employee type restored successfully." : "Employee type deactivated successfully."
            });

        var response = Problem(
            statusCode: result == EmployeeTypeStatusChangeResult.NotFound ? 404 : 409,
            detail: result switch
            {
                EmployeeTypeStatusChangeResult.AlreadyInactive => "The employee type is already inactive.",
                EmployeeTypeStatusChangeResult.AlreadyActive => "The employee type is already active.",
                _ => "Employee type not found."
            });
        ((ProblemDetails)response.Value!).Extensions["result"] = result.ToString();
        return response;
    }

    private ObjectResult Missing() => Problem(statusCode: 404, detail: "Employee type not found.");

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        catch (InvalidOperationException ex) { return Problem(statusCode: 409, detail: ex.Message); }
    }
}
