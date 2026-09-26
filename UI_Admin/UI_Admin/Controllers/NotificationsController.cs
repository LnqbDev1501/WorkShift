using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
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
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
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
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        var model = new SendNotificationViewModel { Channels = ["InApp"] };
        await PopulateAudienceOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendNotificationViewModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAudienceOptionsAsync(request, cancellationToken);
            return View(request);
        }

        try
        {
            var result = await _notificationApiService.BroadcastAsync(request, cancellationToken);
            TempData["NotificationSuccess"] =
                $"Notification #{result.NotificationId} sent to {result.RecipientCount} recipient(s).";
            return RedirectToAction(nameof(Send));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to broadcast notification");
            request.ErrorMessage = "The notification could not be sent. Verify the selected recipients and try again.";
            await PopulateAudienceOptionsAsync(request, cancellationToken);
            return View(request);
        }
    }

    private async Task PopulateAudienceOptionsAsync(SendNotificationViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var options = await _notificationApiService.GetAudienceOptionsAsync(cancellationToken);
            model.Regions = options.Regions;
            model.Branches = options.Branches;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve notification audience options");
            model.ErrorMessage ??= "Audience options are temporarily unavailable.";
        }
    }
}
