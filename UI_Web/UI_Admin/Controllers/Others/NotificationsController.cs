using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN, MANAGER, ACCOUNTANT, STAFF")]
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
            return RedirectToAction(nameof(BroadcastHistory));
        }

        return RedirectToAction(nameof(Personal));
    }

    [HttpGet]
    public async Task<IActionResult> Personal([FromQuery] PersonalNotificationListPageViewModel query, CancellationToken cancellationToken)
    {
        if (User.IsInRole("ADMIN"))
        {
            return RedirectToAction(nameof(BroadcastHistory));
        }

        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
            var result = await _notificationApiService.GetMyNotificationsAsync(query, cancellationToken);
            result.Search = query.Search;
            result.Filter = query.Filter;
            result.SortBy = query.SortBy;
            result.SortOrder = query.SortOrder;

            try
            {
                ViewBag.UnreadCount = await _notificationApiService.GetUnreadCountAsync(cancellationToken);
            }
            catch
            {
                ViewBag.UnreadCount = 0;
            }

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
                return NotFound("Notification not found or access denied.");

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
            return NotFound("Notification not found.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve personal notification {NotificationId}", id);
            return Problem(detail: $"Personal notification detail is temporarily unavailable. [DIAG {exception.GetType().Name}: {exception.Message}]");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationApiService.MarkAsReadAsync(id, cancellationToken);
            if (!result)
                return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Personal));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to mark notification {NotificationId} as read", id);
            return Problem("Unable to mark notification as read.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationApiService.MarkAllAsReadAsync(cancellationToken);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Personal));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to mark all notifications as read");
            return Problem("Unable to mark all notifications as read.");
        }
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> BroadcastHistory([FromQuery] NotificationListPageViewModel query, CancellationToken cancellationToken)
    {
        try
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = ListPaginationViewModel.DefaultPageSize;
            var result = await _notificationApiService.GetPagedAsync(query, cancellationToken);
            result.Search = query.Search;
            result.DateFrom = query.DateFrom;
            result.DateTo = query.DateTo;
            result.SortBy = query.SortBy;
            result.SortOrder = query.SortOrder;
            return View("BroadcastHistory", result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to retrieve broadcast history");
            query.ErrorMessage = !string.IsNullOrWhiteSpace(exception.Message)
                ? exception.Message
                : "Broadcast history is temporarily unavailable.";
            return View("BroadcastHistory", query);
        }
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public IActionResult History([FromQuery] NotificationListPageViewModel query)
    {
        return RedirectToAction(nameof(BroadcastHistory), new
        {
            query.Page,
            query.Search,
            query.SortOrder,
            query.DateFrom,
            query.DateTo
        });
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
            return RedirectToAction(nameof(BroadcastHistory));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to broadcast notification");
            request.ErrorMessage = !string.IsNullOrWhiteSpace(exception.Message)
                ? exception.Message
                : "The notification could not be sent. Verify the selected recipients and try again.";
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
