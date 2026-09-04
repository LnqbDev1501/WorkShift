using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.Services.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Route("api/admin/shift-templates")]
[ShiftTemplateValidation]
public sealed class AdminShiftTemplateController(ShiftTemplateService service) : ControllerBase
{
    [HttpPost]
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
