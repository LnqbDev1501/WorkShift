using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

/// <summary>
/// Administrative Controller for Regional Minimum Wage operations.
/// </summary>
[Route("api/admin/regional-minimum-wages")]
[ApiController]
[Authorize(Policy = "Admin")]
public class AdminRegionalMinimumWageController : ControllerBase
{
    private readonly IRegionalMinimumWageService _service;

    /// <summary>
    /// Initializes a new instance of <see cref="AdminRegionalMinimumWageController"/>.
    /// </summary>
    /// <param name="service">The regional minimum wage service instance.</param>
    public AdminRegionalMinimumWageController(IRegionalMinimumWageService service)
    {
        _service = service;
    }

    /// <summary>
    /// Retrieves current active (un-replaced) regional minimum wage tiers for dropdown selection.
    /// </summary>
    /// <returns>A list of current regional minimum wage options.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<RegionalMinimumWageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetCurrentWagesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information for a specific regional minimum wage ID.
    /// </summary>
    /// <param name="id">The regional minimum wage identifier.</param>
    /// <returns>Detailed wage tier information or a 404 Not Found response.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegionalMinimumWageDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        var wage = await _service.GetByIdAsync(id);
        if (wage == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "Regional minimum wage not found."
            });
        }

        return Ok(wage);
    }

    /// <summary>
    /// Creates a new regional minimum wage tier.
    /// </summary>
    /// <param name="request">The creation payload.</param>
    /// <returns>The created regional minimum wage details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RegionalMinimumWageDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateRegionalMinimumWageRequest request)
    {
        try
        {
            var callerId = GetCurrentUserId();
            var result = await _service.CreateAsync(request, callerId);
            return CreatedAtAction(nameof(GetById), new { id = result.RegionalMinimumWageId }, result);
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
        catch (UnauthorizedAccessException exception)
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
    /// Updates an existing regional minimum wage by creating a new version replacement and updating referencing regions.
    /// </summary>
    /// <param name="id">The old wage ID being replaced.</param>
    /// <param name="request">The update payload.</param>
    /// <returns>The newly created replacement wage tier details.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RegionalMinimumWageDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRegionalMinimumWageRequest request)
    {
        try
        {
            var callerId = GetCurrentUserId();
            var result = await _service.UpdateAsync(id, request, callerId);
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
        catch (UnauthorizedAccessException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
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

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
