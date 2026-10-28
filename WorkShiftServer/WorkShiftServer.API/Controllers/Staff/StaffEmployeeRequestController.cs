using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Authorize(Roles = "STAFF")]
[Route("api/staff/requests")]
public class StaffEmployeeRequestController : ControllerBase
{
    private readonly IStaffEmployeeRequestService _service;
    private readonly ILogger<StaffEmployeeRequestController> _logger;

    public StaffEmployeeRequestController(
        IStaffEmployeeRequestService service,
        ILogger<StaffEmployeeRequestController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private Guid? GetStaffUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(subject, out var staffUserId) ? staffUserId : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StaffEmployeeRequestListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<StaffEmployeeRequestListItemResponse>>> GetPaged(
        [FromQuery] StaffEmployeeRequestListQuery query,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetPagedAsync(staffId.Value, query, cancellationToken);
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
            _logger.LogError(ex, "Failed to retrieve employee requests for staff {StaffId}", staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Employee requests could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{requestId:int}")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> GetById(
        [FromRoute] int requestId,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetByIdAsync(staffId.Value, requestId, cancellationToken);
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
            _logger.LogError(ex, "Failed to retrieve employee request {RequestId} for staff {StaffId}", requestId, staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The employee request details could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("eligible-shifts")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffEligibleShiftResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StaffEligibleShiftResponse>>> GetEligibleShifts(
        [FromQuery] string requestType,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.GetEligibleShiftsAsync(staffId.Value, requestType, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
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
            _logger.LogError(ex, "Failed to retrieve eligible shifts for staff {StaffId} and requestType {RequestType}", staffId, requestType);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Eligible shifts could not be retrieved. Please try again later." });
        }
    }

    [HttpPost("leave")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> SubmitLeave(
        [FromBody] SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.SubmitLeaveRequestAsync(staffId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            _logger.LogError(ex, "Failed to submit leave request for staff {StaffId}", staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Leave request could not be submitted. Please try again later." });
        }
    }

    [HttpPost("late-arrival")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> SubmitLateArrival(
        [FromBody] SubmitLateArrivalRequestRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.SubmitLateArrivalRequestAsync(staffId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            _logger.LogError(ex, "Failed to submit late arrival request for staff {StaffId}", staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Late arrival request could not be submitted. Please try again later." });
        }
    }

    [HttpPost("early-leave")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> SubmitEarlyLeave(
        [FromBody] SubmitEarlyLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.SubmitEarlyLeaveRequestAsync(staffId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            _logger.LogError(ex, "Failed to submit early leave request for staff {StaffId}", staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Early leave request could not be submitted. Please try again later." });
        }
    }

    [HttpPost("salary-increase")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> SubmitSalaryIncrease(
        [FromBody] SubmitSalaryIncreaseRequestRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.SubmitSalaryIncreaseRequestAsync(staffId.Value, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            _logger.LogError(ex, "Failed to submit salary increase request for staff {StaffId}", staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Salary increase request could not be submitted. Please try again later." });
        }
    }

    [HttpPost("{requestId:int}/cancel")]
    [ProducesResponseType(typeof(StaffEmployeeRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffEmployeeRequestDetailResponse>> Cancel(
        [FromRoute] int requestId,
        CancellationToken cancellationToken)
    {
        var staffId = GetStaffUserId();
        if (!staffId.HasValue)
        {
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        }

        try
        {
            var result = await _service.CancelRequestAsync(staffId.Value, requestId, cancellationToken);
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
            _logger.LogError(ex, "Failed to cancel employee request {RequestId} by staff {StaffId}", requestId, staffId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The request could not be cancelled. Please try again later." });
        }
    }
}
