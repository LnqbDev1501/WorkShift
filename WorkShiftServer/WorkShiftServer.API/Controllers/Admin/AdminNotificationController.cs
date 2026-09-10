using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[Route("api/admin/notifications")]
[ApiController]
public class AdminNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] NotificationListQuery query)
    {
        try
        {
            return Ok(await _notificationService.GetPagedAsync(query));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        return notification == null ? NotFound() : Ok(notification);
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(BroadcastNotificationRequest request)
    {
        try
        {
            return Ok(await _notificationService.BroadcastAsync(request, CurrentUserId()));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
    }
}
