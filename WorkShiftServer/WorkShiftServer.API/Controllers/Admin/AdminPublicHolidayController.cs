using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[AllowAnonymous] // Development only: this module does not require authentication.
[Route("api/public-holidays")]
public sealed class AdminPublicHolidayController : ControllerBase
{
    private readonly IPublicHolidayService _service;

    public AdminPublicHolidayController(IPublicHolidayService service)
        => _service = service;

    // Optional identity for CreatedBy and audit. Never trust a client-supplied UUID.
    // No authenticated principal -> Guid.Empty; the Service handles it as null.
    private Guid OptionalCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Guid.Empty;

        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private static bool IsExpectedError(Exception ex) =>
        ex is ArgumentException or KeyNotFoundException
        || (ex is InvalidOperationException && ex.Data.Contains("code"));

    private IActionResult ErrorResponse(Exception ex)
    {
        var (status, title, defaultCode) = ex switch
        {
            ArgumentException => (400, "Validation", "INVALID_REQUEST"),
            KeyNotFoundException => (404, "NotFound", "HOLIDAY_NOT_FOUND"),
            _ => (409, "Conflict", "HOLIDAY_CONFLICT")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["code"] = ex.Data["code"] as string ?? defaultCode;
        return StatusCode(status, problem);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PublicHolidayListRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetAllAsync(request, OptionalCurrentUserId(), ct));
        }
        catch (Exception ex) when (IsExpectedError(ex))
        {
            return ErrorResponse(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, OptionalCurrentUserId(), ct));
        }
        catch (Exception ex) when (IsExpectedError(ex))
        {
            return ErrorResponse(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePublicHolidaysRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, OptionalCurrentUserId(), ct);
            return StatusCode(StatusCodes.Status201Created, new
            {
                createdCount = result.Count,
                items = result
            });
        }
        catch (Exception ex) when (IsExpectedError(ex))
        {
            return ErrorResponse(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePublicHolidayRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, OptionalCurrentUserId(), ct));
        }
        catch (Exception ex) when (IsExpectedError(ex))
        {
            return ErrorResponse(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, OptionalCurrentUserId(), ct);
            return NoContent();
        }
        catch (Exception ex) when (IsExpectedError(ex))
        {
            return ErrorResponse(ex);
        }
    }
}