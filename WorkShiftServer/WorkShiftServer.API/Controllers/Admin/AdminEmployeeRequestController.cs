using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/requests")]
public class AdminEmployeeRequestController : ControllerBase
{
    private readonly IAdminEmployeeRequestService _requestService;
    private readonly ILogger<AdminEmployeeRequestController> _logger;

    public AdminEmployeeRequestController(
        IAdminEmployeeRequestService requestService,
        ILogger<AdminEmployeeRequestController> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paged list of employee requests (LEAVE, LATE_ARRIVAL, EARLY_LEAVE) for admin with filtering and search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminEmployeeRequestListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<AdminEmployeeRequestListItemResponse>>> GetRequests(
        [FromQuery] AdminEmployeeRequestListQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _requestService.GetPagedAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving employee requests.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Employee requests could not be retrieved. Please try again later." });
        }
    }

    /// <summary>
    /// Retrieves detailed information for a specific employee request ID (LEAVE, LATE_ARRIVAL, EARLY_LEAVE).
    /// </summary>
    [HttpGet("{requestId:int}")]
    [ProducesResponseType(typeof(AdminEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminEmployeeRequestDetailResponse>> GetRequestDetail(
        int requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _requestService.GetByIdAsync(requestId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving employee request {RequestId}.", requestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The employee request could not be retrieved. Please try again later." });
        }
    }
}
