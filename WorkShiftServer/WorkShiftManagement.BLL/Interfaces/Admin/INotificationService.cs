using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface INotificationService
{
    Task<PagedResponse<NotificationListResponse>> GetPagedAsync(NotificationListQuery query);

    Task<NotificationDetailResponse?> GetByIdAsync(int notificationId);

    Task<NotificationAudienceOptionsResponse> GetAudienceOptionsAsync();

    Task<BroadcastNotificationResponse> BroadcastAsync(BroadcastNotificationRequest request);
}
