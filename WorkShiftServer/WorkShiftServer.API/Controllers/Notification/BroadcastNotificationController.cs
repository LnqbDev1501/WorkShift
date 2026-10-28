using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Notification.Request;
using WorkShiftManagement.BLL.Interfaces.Notification;

namespace WorkShiftServer.API.Controllers.Notification;

[Route("api/notifications/broadcast")]
[ApiController]
[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT,STAFF")]
public sealed class BroadcastNotificationController : ControllerBase
{
    private readonly IBroadcastNotificationService _service;

    public BroadcastNotificationController(IBroadcastNotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetBroadcastHistory([FromQuery] PersonalNotificationListQuery query)
    {
        var userId = GetCurrentUserId();
        try
        {
            return Ok(await _service.GetBroadcastHistoryAsync(userId, query));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{notificationId:int}")]
    public async Task<IActionResult> GetById(int notificationId)
    {
        var userId = GetCurrentUserId();
        var notification = await _service.GetBroadcastByIdAsync(userId, notificationId);
        return notification == null ? NotFound() : Ok(notification);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
