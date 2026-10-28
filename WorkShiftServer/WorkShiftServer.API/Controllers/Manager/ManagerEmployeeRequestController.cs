using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/requests")]
public class ManagerEmployeeRequestController : ControllerBase
{
    private readonly IManagerEmployeeRequestService _service;
    private readonly ILogger<ManagerEmployeeRequestController> _logger;

    public ManagerEmployeeRequestController(
        IManagerEmployeeRequestService service,
        ILogger<ManagerEmployeeRequestController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private Guid? GetManagerId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(subject, out var managerId) ? managerId : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ManagerEmployeeRequestListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<ManagerEmployeeRequestListItemResponse>>> GetPaged(
        [FromQuery] ManagerEmployeeRequestListQuery query,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetPagedAsync(managerId.Value, query, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve employee requests for manager {ManagerId}", managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Employee requests could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{requestId:int}")]
    [ProducesResponseType(typeof(ManagerEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ManagerEmployeeRequestDetailResponse>> GetById(
        [FromRoute] int requestId,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetByIdAsync(managerId.Value, requestId, cancellationToken);
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve employee request {RequestId} for manager {ManagerId}", requestId, managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The employee request details could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{requestId:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ManagerEmployeeRequestHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ManagerEmployeeRequestHistoryResponse>>> GetHistory(
        [FromRoute] int requestId,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetHistoryAsync(managerId.Value, requestId, cancellationToken);
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve history for employee request {RequestId} and manager {ManagerId}", requestId, managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The request history could not be retrieved. Please try again later." });
        }
    }

    [HttpPost("{requestId:int}/approve")]
    [ProducesResponseType(typeof(ManagerEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ManagerEmployeeRequestDetailResponse>> Approve(
        [FromRoute] int requestId,
        [FromBody] ApproveEmployeeRequestRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.ApproveAsync(managerId.Value, requestId, request, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve employee request {RequestId} by manager {ManagerId}", requestId, managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The request could not be approved. Please try again later." });
        }
    }

    [HttpPost("{requestId:int}/reject")]
    [ProducesResponseType(typeof(ManagerEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ManagerEmployeeRequestDetailResponse>> Reject(
        [FromRoute] int requestId,
        [FromBody] RejectEmployeeRequestRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.RejectAsync(managerId.Value, requestId, request, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject employee request {RequestId} by manager {ManagerId}", requestId, managerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The request could not be rejected. Please try again later." });
        }
    }
}
