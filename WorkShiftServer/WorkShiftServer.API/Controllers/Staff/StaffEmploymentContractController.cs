using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<IReadOnlyList<StaffContractListItemResponse>>> GetMyContracts(CancellationToken cancellationToken)
    {
        var staffUserId = GetStaffUserId();
        if (!staffUserId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetMyContractsAsync(staffUserId.Value, cancellationToken));
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
}
