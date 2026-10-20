using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "MANAGER")]
[Route("api/manager/contracts")]
public class ManagerEmploymentContractController(
    IManagerEmploymentContractService service, ILogger<ManagerEmploymentContractController> logger) : ControllerBase
{
    private Guid? GetManagerId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");
        return Guid.TryParse(subject, out var managerId) ? managerId : null;
    }

    [HttpGet("staff-options")]
    [ProducesResponseType(typeof(IReadOnlyList<ContractStaffOptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ContractStaffOptionResponse>>> StaffOptions(
        [FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.GetStaffOptionsAsync(managerId.Value, mode, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load contract staff options.");
            return StatusCode(500, new { message = "Staff options could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("branch-minimum-wage")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<decimal>> GetBranchMinimumWage(CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.GetBranchMinimumWageAsync(managerId.Value, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get branch minimum wage.");
            return Ok(0m);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ManagerEmploymentContractListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ManagerEmploymentContractListItemResponse>>> GetContracts(
        [FromQuery] ManagerEmploymentContractListRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.GetPagedAsync(managerId.Value, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load manager employment contracts.");
            return StatusCode(500, new { message = "Employment contracts could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ManagerEmploymentContractDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerEmploymentContractDetailResponse>> GetById(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.GetByIdAsync(managerId.Value, id, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load manager employment contract {ContractId}", id);
            return StatusCode(500, new { message = "The employment contract details could not be retrieved. Please try again later." });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateEmploymentContractResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateEmploymentContractResponse>> Create(
        [FromBody] CreateEmploymentContractRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(managerId.Value, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create an employment contract for staff {StaffUserId}", request.StaffUserId);
            return StatusCode(500, new { message = "The employment contract could not be created. Please try again later." });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ManagerEmploymentContractDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagerEmploymentContractDetailResponse>> Update(
        [FromRoute] int id, [FromBody] UpdateEmploymentContractRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.UpdateDraftAsync(managerId.Value, id, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update draft employment contract {ContractId}", id);
            return StatusCode(500, new { message = "The employment contract could not be updated. Please try again later." });
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ManagerEmploymentContractDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagerEmploymentContractDetailResponse>> Cancel(
        [FromRoute] int id, [FromBody] CancelEmploymentContractRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.CancelContractAsync(managerId.Value, id, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel employment contract {ContractId}", id);
            return StatusCode(500, new { message = "The employment contract could not be cancelled. Please try again later." });
        }
    }

    [HttpPost("{id:int}/terminate")]
    [ProducesResponseType(typeof(ManagerEmploymentContractDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagerEmploymentContractDetailResponse>> Terminate(
        [FromRoute] int id, [FromBody] TerminateEmploymentContractRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.TerminateContractAsync(managerId.Value, id, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to terminate employment contract {ContractId}", id);
            return StatusCode(500, new { message = "The employment contract could not be terminated. Please try again later." });
        }
    }

    [HttpGet("{id:int}/pdf/preview")]
    public async Task<IActionResult> PreviewPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            var pdfBytes = await service.GetPdfPreviewAsync(managerId.Value, id, cancellationToken);
            return File(pdfBytes, "application/pdf");
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF preview for contract {ContractId}", id);
            return StatusCode(500, new { message = "The PDF preview could not be generated." });
        }
    }

    [HttpPost("{id:int}/send")]
    public async Task<ActionResult<ManagerEmploymentContractDetailResponse>> Send([FromRoute] int id, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return Ok(await service.SendContractAsync(managerId.Value, id, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contract {ContractId} to staff", id);
            return StatusCode(500, new { message = "The contract could not be sent to staff." });
        }
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            var (bytes, contentType, fileName) = await service.GetPdfFileAsync(managerId.Value, id, isDownload: false, cancellationToken);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, contentType);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(500, new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve PDF for contract {ContractId}", id);
            return StatusCode(500, new { message = "The contract PDF could not be retrieved." });
        }
    }

    [HttpGet("{id:int}/pdf/download")]
    public async Task<IActionResult> DownloadPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            var (bytes, contentType, fileName) = await service.GetPdfFileAsync(managerId.Value, id, isDownload: true, cancellationToken);
            return File(bytes, contentType, fileName);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(500, new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download PDF for contract {ContractId}", id);
            return StatusCode(500, new { message = "The contract PDF could not be downloaded." });
        }
    }

    // Contract Change Requests
    [HttpGet("change-requests")]
    public async Task<ActionResult<PagedResponse<ManagerContractChangeRequestResponse>>> GetChangeRequests(
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetChangeRequestsAsync(managerId.Value, status, pageNumber, pageSize, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract change requests for manager {ManagerId}", managerId.Value);
            return StatusCode(500, new { message = "Could not retrieve contract change requests." });
        }
    }

    [HttpGet("change-requests/{changeRequestId:int}")]
    public async Task<ActionResult<ManagerContractChangeRequestResponse>> GetChangeRequestById(
        [FromRoute] int changeRequestId, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetChangeRequestByIdAsync(managerId.Value, changeRequestId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract change request {ChangeRequestId}", changeRequestId);
            return StatusCode(500, new { message = "Could not retrieve change request detail." });
        }
    }

    [HttpPost("change-requests/{changeRequestId:int}/approve")]
    public async Task<ActionResult<ManagerContractChangeRequestResponse>> ApproveChangeRequest(
        [FromRoute] int changeRequestId,
        [FromBody] ApproveContractChangeRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.ApproveChangeRequestAsync(managerId.Value, changeRequestId, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to approve contract change request {ChangeRequestId}", changeRequestId);
            return StatusCode(500, new { message = "Could not approve contract change request." });
        }
    }

    [HttpPost("change-requests/{changeRequestId:int}/reject")]
    public async Task<ActionResult<ManagerContractChangeRequestResponse>> RejectChangeRequest(
        [FromRoute] int changeRequestId,
        [FromBody] RejectContractChangeRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.RejectChangeRequestAsync(managerId.Value, changeRequestId, request, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reject contract change request {ChangeRequestId}", changeRequestId);
            return StatusCode(500, new { message = "Could not reject contract change request." });
        }
    }

    // History
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ManagerContractHistoryResponse>> GetContractHistory(
        [FromRoute] int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = "desc",
        CancellationToken cancellationToken = default)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetContractHistoryAsync(managerId.Value, id, pageNumber, pageSize, sort, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get history for contract {ContractId}", id);
            return StatusCode(500, new { message = "Could not retrieve contract history." });
        }
    }

    [HttpGet("staff/{staffUserId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<StaffContractHistoryItemResponse>>> GetStaffContractHistory(
        [FromRoute] Guid staffUserId, CancellationToken cancellationToken)
    {
        var managerId = GetManagerId();
        if (!managerId.HasValue)
            return Unauthorized(new { message = "A valid authenticated user ID is required." });

        try
        {
            return Ok(await service.GetStaffContractHistoryAsync(managerId.Value, staffUserId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get contract history for staff {StaffUserId}", staffUserId);
            return StatusCode(500, new { message = "Could not retrieve staff contract history." });
        }
    }
}
