using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN, MANAGER, ACCOUNTANT")]
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
    public IActionResult Index()
    {
        if (User.IsInRole("ADMIN"))
        {
            return RedirectToAction(nameof(History));
        }

        return RedirectToAction(nameof(Personal));
    }

    [HttpGet]
    public async Task<IActionResult> Personal([FromQuery] PersonalNotificationListPageViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
            var result = await _notificationApiService.GetMyNotificationsAsync(query, cancellationToken);
            result.Search = query.Search;
            result.Filter = query.Filter;
            result.SortBy = query.SortBy;
            result.SortOrder = query.SortOrder;
            return View(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve personal notifications");
            query.ErrorMessage = "Personal notifications are temporarily unavailable.";
            return View(query);
        }
    }

    [HttpGet]
    public async Task<IActionResult> PersonalDetail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _notificationApiService.GetMyNotificationByIdAsync(id, cancellationToken);
            if (notification == null)
                return NotFound("Không tìm thấy thông báo hoặc bạn không có quyền truy cập.");

            if (!notification.IsRead)
            {
                await _notificationApiService.MarkAsReadAsync(id, cancellationToken);
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            return View(notification);
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Không tìm thấy thông báo.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve personal notification {NotificationId}", id);
            return Problem(detail: $"Personal notification detail is temporarily unavailable. [DIAG {exception.GetType().Name}: {exception.Message}]");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationApiService.MarkAsReadAsync(id, cancellationToken);
            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Personal));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to mark notification {NotificationId} as read", id);
            return Problem("Unable to mark notification as read.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> BroadcastHistory([FromQuery] BroadcastHistoryPageViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
            var result = await _notificationApiService.GetBroadcastHistoryAsync(query, cancellationToken);
            result.Search = query.Search;
            result.Type = query.Type;
            result.DateFrom = query.DateFrom;
            result.DateTo = query.DateTo;
            result.SortBy = query.SortBy;
            result.SortOrder = query.SortOrder;
            return View(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve broadcast history");
            query.ErrorMessage = "Broadcast history is temporarily unavailable.";
            return View(query);
        }
    }

    [HttpGet]
    public async Task<IActionResult> BroadcastDetail(int id, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _notificationApiService.GetBroadcastByIdAsync(id, cancellationToken);
            if (notification == null)
                return NotFound("Không tìm thấy thông báo broadcast.");

            if (!notification.IsRead && notification.UserNotificationId > 0)
            {
                await _notificationApiService.MarkAsReadAsync(notification.UserNotificationId, cancellationToken);
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            return View("BroadcastDetail", notification);
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Không tìm thấy thông báo broadcast.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve broadcast notification {NotificationId}", id);
            return Problem(detail: $"Broadcast notification detail is temporarily unavailable. [DIAG {exception.GetType().Name}: {exception.Message}]");
        }
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> History([FromQuery] NotificationListPageViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
            var result = await _notificationApiService.GetPagedAsync(query, cancellationToken);
            result.Search = query.Search;
            result.Type = query.Type;
            result.DateFrom = query.DateFrom;
            result.DateTo = query.DateTo;
            result.SortBy = query.SortBy;
            result.SortOrder = query.SortOrder;
            return View("List", result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve notification history");
            query.ErrorMessage = "Notification history is temporarily unavailable.";
            return View("List", query);
        }
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
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
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        var model = new SendNotificationViewModel { Channels = ["InApp"] };
        await PopulateAudienceOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
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
