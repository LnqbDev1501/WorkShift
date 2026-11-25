using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[Route("api/admin/notifications")]
[ApiController]
[Authorize(Policy = "Admin")]
public class AdminNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
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

    [HttpGet("audience-options")]
    public async Task<IActionResult> GetAudienceOptions()
    {
        return Ok(await _notificationService.GetAudienceOptionsAsync());
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(BroadcastNotificationRequest request)
    {
        try
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            Guid? senderUserId = Guid.TryParse(subject, out var parsedId) ? parsedId : null;
            var senderEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

            return Ok(await _notificationService.BroadcastAsync(request, senderUserId, senderEmail));
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
