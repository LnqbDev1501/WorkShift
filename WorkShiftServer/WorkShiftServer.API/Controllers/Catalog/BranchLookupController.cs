using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Catalog;

/// <summary>
/// Shared branch lookup used by Admin, Manager and Accountant screens.
/// </summary>
[Route("api/branches")]
[ApiController]
[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
public class BranchLookupController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchLookupController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    /// <summary>
    /// Retrieves active branches available for filtering and reporting.
    /// </summary>
    [HttpGet("assignable")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchAssignableOptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<BranchAssignableOptionResponse>>> GetAssignableOptions()
    {
        try
        {
            var result = await _branchService.GetAssignableOptionsAsync();
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Failed to retrieve assignable branches."
            });
        }
    }
}
