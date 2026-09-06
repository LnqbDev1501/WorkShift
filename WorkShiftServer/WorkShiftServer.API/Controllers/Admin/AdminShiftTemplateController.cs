using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Services.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Route("api/admin/shift-templates")]
public sealed class AdminShiftTemplateController(
    ShiftTemplateService service, ILogger<AdminShiftTemplateController> logger) : ControllerBase
{
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Restore(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var restored = await service.RestoreAsync(id, cancellationToken);
            if (!restored)
                return Ok(new { message = "Shift template is already ACTIVE." });

            return Ok(new { message = "Shift template restored successfully." });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to restore shift template {ShiftTemplateId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Unable to restore the shift template. Please try again later." });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deactivated = await service.DeleteAsync(id, cancellationToken);
            if (!deactivated)
                return Ok(new { message = "Shift template is already INACTIVE." });

            return Ok(new { message = "Shift template deactivated successfully." });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to deactivate shift template {ShiftTemplateId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Unable to deactivate the shift template. Please try again later." });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ShiftTemplateListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ShiftTemplateListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShiftTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var template = await service.GetByIdAsync(id, cancellationToken);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    [ShiftTemplateValidation]
    public async Task<IActionResult> Create(CreateShiftTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await service.CreateAsync(request, cancellationToken);
            if (template is null)
                return Conflict(new { message = "A shift template with this name already exists." });

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Shift template created successfully.",
                data = new
                {
                    template.ShiftTemplateId,
                    template.ShiftName,
                    template.StartTime,
                    template.EndTime,
                    template.BreakMinutes,
                    template.Description,
                    template.Status,
                    template.CreatedAt,
                    template.UpdatedAt
                }
            });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ShiftTemplateValidation]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateShiftTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await service.UpdateAsync(id, request, cancellationToken);
            if (template is null)
                return Conflict(new { message = "A shift template with this name already exists." });

            return Ok(new { message = "Shift template updated successfully.", data = template });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

internal sealed class ShiftTemplateValidationAttribute : ActionFilterAttribute
{
    // Run before ApiController's automatic ModelStateInvalidFilter (order -2000).
    public ShiftTemplateValidationAttribute() => Order = -2001;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var error = context.ModelState
            .Where(entry => entry.Value!.Errors.Count > 0)
            .OrderByDescending(entry => entry.Key.StartsWith('$'))
            .First();
        var message = error.Key.StartsWith('$') || error.Value!.Errors[0].Exception is not null
            ? CreateShiftTemplateRequest.GetBodyError(error.Key.TrimStart('$', '.'))
            : string.IsNullOrEmpty(error.Key) || error.Key == "request"
                ? "The request body is required. Send a JSON object containing shiftName, startTime, endTime and breakMinutes."
                : error.Value!.Errors[0].ErrorMessage;

        context.Result = new BadRequestObjectResult(new { message });
    }
}
