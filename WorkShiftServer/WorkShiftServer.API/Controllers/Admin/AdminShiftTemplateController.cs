using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/shift-templates")]
[ShiftTemplateValidation]
[TypeFilter(typeof(ShiftTemplateExceptionFilter))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public sealed class AdminShiftTemplateController(
    IShiftTemplateService service) : ControllerBase
{
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        CancellationToken cancellationToken)
    {
        var restored = await service.RestoreAsync(id, cancellationToken);
        if (!restored)
            return Ok(new { message = "Shift template is already ACTIVE." });

        return Ok(new { message = "Shift template restored successfully." });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        CancellationToken cancellationToken)
    {
        var deactivated = await service.DeleteAsync(id, cancellationToken);
        if (!deactivated)
            return Ok(new { message = "Shift template is already INACTIVE." });

        return Ok(new { message = "Shift template deactivated successfully." });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ShiftTemplateListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ShiftTemplateListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShiftTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        CancellationToken cancellationToken)
    {
        var template = await service.GetByIdAsync(id, cancellationToken);
        return template is null ? NotFound(new { message = "Shift template not found." }) : Ok(template);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateShiftTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await service.CreateAsync(request, cancellationToken);
        if (template is null)
            return Conflict(new { message = "A shift template with this name already exists." });

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Shift template created successfully.",
            data = template
        });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute, Range(1, int.MaxValue, ErrorMessage = "id must be a positive integer.")] int id,
        UpdateShiftTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await service.UpdateAsync(id, request, cancellationToken);
        if (template is null)
            return Conflict(new { message = "A shift template with this name already exists." });

        return Ok(new { message = "Shift template updated successfully.", data = template });
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
        var field = error.Key;
        var modelError = error.Value!.Errors[0];
        string message;

        if (field.Equals("id", StringComparison.OrdinalIgnoreCase))
            message = "id must be a positive integer.";
        else if (field.Equals("Page", StringComparison.OrdinalIgnoreCase))
            message = "Page must be a whole number between 1 and 20.";
        else if (field.StartsWith('$') || modelError.Exception is not null)
            message = CreateShiftTemplateRequest.GetBodyError(field.TrimStart('$', '.'));
        else if (string.IsNullOrEmpty(field) || field == "request")
            message = "The request body is required. Send a JSON object containing shiftName, startTime and endTime.";
        else
            message = modelError.ErrorMessage;

        context.Result = new BadRequestObjectResult(new { message });
    }
}

internal sealed class ShiftTemplateExceptionFilter(ILogger<ShiftTemplateExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // A disconnected client must not be reported as a server failure.
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested)
            return;

        var (statusCode, message) = context.Exception switch
        {
            ValidationException exception => (StatusCodes.Status400BadRequest, exception.Message),
            KeyNotFoundException exception => (StatusCodes.Status404NotFound, exception.Message),
            _ => (StatusCodes.Status500InternalServerError,
                "Unable to process the shift template request. Please try again later.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(context.Exception, "Shift template request failed: {Method} {Path}.",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);

        context.Result = new ObjectResult(new { message }) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
