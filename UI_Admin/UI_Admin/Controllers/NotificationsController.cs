using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public sealed class NotificationsController : Controller
{
    private readonly NotificationApiService _notificationApiService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(NotificationApiService notificationApiService, ILogger<NotificationsController> logger)
    {
        _notificationApiService = notificationApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] NotificationListPageViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize, 1, 100);
            return View(await _notificationApiService.GetPagedAsync(query, cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve notification history");
            query.ErrorMessage = "Notification history is temporarily unavailable.";
            return View(query);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _notificationApiService.GetByIdAsync(id, cancellationToken);
            return notification == null ? NotFound() : View(notification);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve notification {NotificationId}", id);
            return Problem("Notification detail is temporarily unavailable.");
        }
    }

    [HttpGet]
    public IActionResult Send() => View(new SendNotificationViewModel { Channels = ["InApp"] });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendNotificationViewModel request, CancellationToken cancellationToken)
    {
        if ((request.Target == "Region" && !request.RegionId.HasValue) ||
            (request.Target == "Branch" && !request.BranchId.HasValue))
        {
            ModelState.AddModelError(string.Empty, "Enter the target Region ID or Branch ID for the selected scope.");
        }

        if (!ModelState.IsValid)
            return View(request);

        try
        {
            request.Result = await _notificationApiService.BroadcastAsync(request, cancellationToken);
            return View(request);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to broadcast notification");
            request.ErrorMessage = "The notification could not be sent. Verify the selected recipients and try again.";
            return View(request);
        }
    }
}
