using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Request;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController]
[Authorize(Policy = "Accountant")]

[Route("api/Accountant/salary-histories")]
public sealed class AccountantSalaryHistoryController : ControllerBase
{
    private readonly ISalaryHistoryService _service;

    public AccountantSalaryHistoryController(ISalaryHistoryService service) => _service = service;

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated Accountant ID is invalid.");
        return id;
    }

    private IActionResult ErrorResponse(Exception exception)
    {
        var (status, title, code) = exception switch
        {
            UnauthorizedAccessException => (403, "Forbidden", "SALARY_HISTORY_ACCESS_DENIED"),
            KeyNotFoundException => (404, "NotFound", "SALARY_HISTORY_NOT_FOUND"),
            _ => (400, "Validation", "INVALID_SALARY_HISTORY_QUERY")
        };
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        return StatusCode(status, problem);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SalaryHistoryListRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.GetAllAsync(request, CurrentUserId(), ct)); }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException)
        { return ErrorResponse(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try { return Ok(await _service.GetByIdAsync(id, CurrentUserId(), ct)); }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException)
        { return ErrorResponse(ex); }
    }
}
