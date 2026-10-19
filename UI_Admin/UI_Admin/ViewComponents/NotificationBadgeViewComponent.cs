using Microsoft.AspNetCore.Mvc;
using UI_Admin.Services;

namespace UI_Admin.ViewComponents;

public sealed class NotificationBadgeViewComponent : ViewComponent
{
    private readonly NotificationApiService _notificationApiService;
    private readonly ILogger<NotificationBadgeViewComponent> _logger;

    public NotificationBadgeViewComponent(
        NotificationApiService notificationApiService,
        ILogger<NotificationBadgeViewComponent> logger)
    {
        _notificationApiService = notificationApiService;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var count = 0;

        try
        {
            count = await _notificationApiService.GetUnreadCountAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load the unread notification count.");
        }

        return View(count);
    }
}
