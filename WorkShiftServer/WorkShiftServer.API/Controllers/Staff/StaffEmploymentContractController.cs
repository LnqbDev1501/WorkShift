using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;

namespace WorkShiftServer.API.Controllers.Staff;

[ApiController]
[Authorize(Roles = "STAFF")]
[Route("api/staff/contracts")]
public class StaffEmploymentContractController(
    IStaffEmploymentContractService service,
    ILogger<StaffEmploymentContractController> logger) : ControllerBase
{
    private Guid? GetStaffUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");
        return Guid.TryParse(subject, out var staffUserId) ? staffUserId : null;
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            var (bytes, contentType, fileName) = await service.GetStaffPdfFileAsync(staffUserId.Value, id, isDownload: false, cancellationToken);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, contentType);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve PDF for staff contract {ContractId}", id);
            return StatusCode(500, new { message = "The contract PDF could not be retrieved." });
        }
    }

    [HttpGet("{id:int}/pdf/download")]
    public async Task<IActionResult> DownloadPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            var (bytes, contentType, fileName) = await service.GetStaffPdfFileAsync(staffUserId.Value, id, isDownload: true, cancellationToken);
            return File(bytes, contentType, fileName);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download PDF for staff contract {ContractId}", id);
            return StatusCode(500, new { message = "The contract PDF could not be downloaded." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffContractListItemResponse>>> GetMyContracts(
        [FromQuery] StaffContractListQuery query, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetMyContractsAsync(staffUserId.Value, query, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contracts for staff {StaffUserId}", staffUserId.Value);
            return StatusCode(500, new { message = "Could not retrieve staff employment contracts." });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StaffContractDetailResponse>> GetMyContractById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetMyContractByIdAsync(staffUserId.Value, id, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract detail {ContractId} for staff {StaffUserId}", id, staffUserId.Value);
            return StatusCode(500, new { message = "Could not retrieve staff contract details." });
        }
    }

    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<StaffContractDetailResponse>> AcceptContract([FromRoute] int id, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.AcceptContractAsync(staffUserId.Value, id, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to accept contract {ContractId} for staff {StaffUserId}", id, staffUserId.Value);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/change-requests")]
    [ProducesResponseType(typeof(StaffContractChangeRequestResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<StaffContractChangeRequestResponse>> SubmitChangeRequest(
        [FromRoute] int id,
        [FromBody] SubmitContractChangeRequestRequest request,
        CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            var result = await service.SubmitChangeRequestAsync(staffUserId.Value, id, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            logger.LogError(ex, "Database update error submitting change request for contract {ContractId} by staff {StaffUserId}", id, staffUserId.Value);
            return StatusCode(500, new { message = "Lỗi khi lưu yêu cầu thay đổi hợp đồng vào cơ sở dữ liệu." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit change request for contract {ContractId} by staff {StaffUserId}", id, staffUserId.Value);
            return StatusCode(500, new { message = "Không thể gửi yêu cầu thay đổi hợp đồng. Vui lòng thử lại sau." });
        }
    }

    [HttpGet("{id:int}/change-requests")]
    public async Task<ActionResult<IReadOnlyList<StaffContractChangeRequestResponse>>> GetMyContractChangeRequests(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetMyContractChangeRequestsAsync(staffUserId.Value, id, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract change requests for contract {ContractId}", id);
            return StatusCode(500, new { message = "Could not retrieve contract change requests." });
        }
    }

    [HttpGet("change-requests/{changeRequestId:int}")]
    public async Task<ActionResult<StaffContractChangeRequestResponse>> GetMyChangeRequestById(
        [FromRoute] int changeRequestId, CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetMyChangeRequestByIdAsync(staffUserId.Value, changeRequestId, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract change request {ChangeRequestId}", changeRequestId);
            return StatusCode(500, new { message = "Could not retrieve change request detail." });
        }
    }

    [HttpGet("can-register-shift")]
    public async Task<ActionResult<object>> CanRegisterShift(
        [FromQuery] int branchId,
        [FromQuery] DateTime startAt,
        [FromQuery] DateTime endAt,
        CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            var canRegister = await service.CanStaffRegisterShiftAsync(staffUserId.Value, branchId, startAt, endAt, cancellationToken);
            return Ok(new { canRegister, staffUserId = staffUserId.Value, branchId, startAt, endAt });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check shift registration for staff {StaffUserId}", staffUserId.Value);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the list of active employee positions/types from the system for staff contract change requests.
    /// </summary>
    [HttpGet("active-positions")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetActivePositions(CancellationToken cancellationToken)
    {
        try
        {
            var positions = await service.GetActivePositionNamesAsync(cancellationToken);
            return Ok(positions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve active positions for staff.");
            return StatusCode(500, new { message = "Could not retrieve active position options." });
        }
    }
}
