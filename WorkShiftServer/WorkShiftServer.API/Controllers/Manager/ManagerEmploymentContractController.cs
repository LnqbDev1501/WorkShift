using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet("staff-options")]
    [ProducesResponseType(typeof(IReadOnlyList<ContractStaffOptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContractStaffOptionResponse>>> StaffOptions(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var managerId))
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try { return Ok(await service.GetStaffOptionsAsync(managerId, cancellationToken)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load contract staff options.");
            return StatusCode(500, new { message = "Staff options could not be retrieved. Please try again later." });
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
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var managerId))
            return Unauthorized(new { message = "A valid authenticated user ID is required." });
        try
        {
            return StatusCode(StatusCodes.Status201Created, await service.CreateAsync(managerId, request, cancellationToken));
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
}
