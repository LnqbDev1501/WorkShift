using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

/// <summary>
/// Administrative Controller for Region Management operations.
/// </summary>
[Route("api/admin/regions")]
[ApiController]
public class AdminRegionController : ControllerBase
{
    private readonly IRegionService _regionService;

    /// <summary>
    /// Initializes a new instance of <see cref="AdminRegionController"/>.
    /// </summary>
    /// <param name="regionService">The region service instance.</param>
    public AdminRegionController(IRegionService regionService)
    {
        _regionService = regionService;
    }

    /// <summary>
    /// Retrieves a paginated list of regions with optional filtering and search parameters.
    /// </summary>
    /// <param name="query">Filtering, search, and pagination parameters.</param>
    /// <returns>A paginated list of region items.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RegionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaged([FromQuery] RegionListQuery query)
    {
        try
        {
            var result = await _regionService.GetPagedAsync(query);
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
    /// Retrieves detailed information for a specific region including its associated branches.
    /// </summary>
    /// <param name="id">The unique identifier of the region.</param>
    /// <returns>Detailed region information or a 404 Not Found response.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var region = await _regionService.GetByIdAsync(id);
        if (region == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "Region not found."
            });
        }

        return Ok(region);
    }

    /// <summary>
    /// Creates a new region record.
    /// </summary>
    /// <param name="request">The creation payload containing region details.</param>
    /// <returns>The created region object with a 201 Created status and location header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RegionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateRegionRequest request)
    {
        try
        {
            var result = await _regionService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.RegionId }, result);
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
    /// Updates the name and description of an existing region.
    /// </summary>
    /// <param name="id">The unique identifier of the region to update.</param>
    /// <param name="request">The update payload.</param>
    /// <returns>The updated region details.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RegionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRegionRequest request)
    {
        try
        {
            var result = await _regionService.UpdateAsync(id, request);
            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Region not found."
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
    /// Deactivates (soft deletes) an active region by setting its status to INACTIVE.
    /// </summary>
    /// <param name="id">The unique identifier of the region to deactivate.</param>
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
            var success = await _regionService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Region not found."
                });
            }

            return Ok(new { message = "Region deactivated successfully." });
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
    /// Restores an inactive region by setting its status back to ACTIVE.
    /// </summary>
    /// <param name="id">The unique identifier of the region to restore.</param>
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
            var success = await _regionService.RestoreAsync(id);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Region not found."
                });
            }

            return Ok(new { message = "Region restored successfully." });
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
}
