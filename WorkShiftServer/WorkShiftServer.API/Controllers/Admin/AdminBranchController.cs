using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

/// <summary>
/// Administrative Controller for Branch Management operations.
/// </summary>
[Route("api/admin/branches")]
[ApiController]
[Authorize(Policy = "Admin")]
public class AdminBranchController : ControllerBase
{
    private readonly IBranchService _branchService;

    /// <summary>
    /// Initializes a new instance of <see cref="AdminBranchController"/>.
    /// </summary>
    /// <param name="branchService">The branch service instance.</param>
    public AdminBranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    /// <summary>
    /// Retrieves a paginated list of branches with optional search, region, and status filters.
    /// </summary>
    /// <param name="query">Filtering, search, and pagination parameters.</param>
    /// <returns>A paginated list of branch items.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BranchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaged([FromQuery] BranchListQuery query)
    {
        try
        {
            var result = await _branchService.GetPagedAsync(query);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Retrieves detailed information for a specific branch.
    /// </summary>
    /// <param name="id">The unique identifier of the branch.</param>
    /// <returns>Detailed branch information or a 404 Not Found response.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BranchDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var branch = await _branchService.GetByIdAsync(id);
        if (branch == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "Branch not found."
            });
        }

        return Ok(branch);
    }

    /// <summary>
    /// Creates a new branch record.
    /// </summary>
    /// <param name="request">The creation payload containing branch details.</param>
    /// <returns>The created branch object with a 201 Created status and location header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request)
    {
        try
        {
            var result = await _branchService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.BranchId }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Updates general information for an existing branch. Status and BranchCode cannot be updated via this endpoint.
    /// </summary>
    /// <param name="id">The unique identifier of the branch to update.</param>
    /// <param name="request">The update payload.</param>
    /// <returns>The updated branch details.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BranchDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBranchRequest request)
    {
        try
        {
            var result = await _branchService.UpdateAsync(id, request);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Branch not found."
                });
            }

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Deactivates (soft deletes) an active branch by setting its status to INACTIVE.
    /// </summary>
    /// <param name="id">The unique identifier of the branch to deactivate.</param>
    /// <returns>A confirmation message upon successful deactivation.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _branchService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Branch not found."
                });
            }

            return Ok(new { message = "Branch deactivated successfully." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Retrieves a paginated list of current employees assigned to a specific branch.
    /// </summary>
    /// <param name="id">The unique identifier of the branch.</param>
    /// <param name="query">Filtering, search, and pagination parameters for employees.</param>
    /// <returns>The list of employees belonging to the branch or a 404 Not Found response.</returns>
    [HttpGet("{id:int}/employees")]
    [ProducesResponseType(typeof(BranchEmployeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEmployees(int id, [FromQuery] BranchEmployeeListQuery query)
    {
        try
        {
            var result = await _branchService.GetEmployeesAsync(id, query);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Branch not found."
                });
            }

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Restores an inactive branch by setting its status back to ACTIVE, provided its region is active.
    /// </summary>
    /// <param name="id">The unique identifier of the branch to restore.</param>
    /// <returns>A confirmation message upon successful restoration.</returns>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            var success = await _branchService.RestoreAsync(id);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Branch not found."
                });
            }

            return Ok(new { message = "Branch restored successfully." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
    }

    /// <summary>
    /// Retrieves active branches with manager and accountant counts for staff provisioning.
    /// </summary>
    /// <returns>A list of assignable branch options.</returns>
    [HttpGet("assignable")]
    [Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
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
