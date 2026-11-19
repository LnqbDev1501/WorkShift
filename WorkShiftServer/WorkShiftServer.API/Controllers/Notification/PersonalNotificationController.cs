using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Notification.Request;
using WorkShiftManagement.BLL.Interfaces.Notification;

namespace WorkShiftServer.API.Controllers.Notification;

[Route("api/notifications/personal")]
[ApiController]
[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT,STAFF")]
public sealed class PersonalNotificationController : ControllerBase
{
    private readonly IPersonalNotificationService _service;

    public PersonalNotificationController(IPersonalNotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] PersonalNotificationListQuery query)
    {
        var userId = GetCurrentUserId();
        try
        {
            return Ok(await _service.GetMyNotificationsAsync(userId, query));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _service.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpGet("{userNotificationId:int}")]
    public async Task<IActionResult> GetById(int userNotificationId)
    {
        var userId = GetCurrentUserId();
        var notification = await _service.GetMyNotificationByIdAsync(userId, userNotificationId);
        return notification == null ? NotFound() : Ok(notification);
    }

    [HttpPost("{userNotificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int userNotificationId)
    {
        var userId = GetCurrentUserId();
        var result = await _service.MarkAsReadAsync(userId, userNotificationId);
        return result ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var count = await _service.MarkAllAsReadAsync(userId);
        return Ok(new { success = true, count });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}