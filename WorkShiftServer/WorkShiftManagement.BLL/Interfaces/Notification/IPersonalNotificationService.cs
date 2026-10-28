using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Notification.Request;
using WorkShiftManagement.BLL.DTOs.Notification.Response;

namespace WorkShiftManagement.BLL.Interfaces.Notification;

public interface IPersonalNotificationService
{
    Task<PagedResponse<PersonalNotificationListResponse>> GetMyNotificationsAsync(Guid userId, PersonalNotificationListQuery query);

    Task<PersonalNotificationDetailResponse?> GetMyNotificationByIdAsync(Guid userId, int userNotificationId);

    Task<bool> MarkAsReadAsync(Guid userId, int userNotificationId);

    Task<int> GetUnreadCountAsync(Guid userId);
}

public interface IBroadcastNotificationService
{
    Task<PagedResponse<BroadcastNotificationListResponse>> GetBroadcastHistoryAsync(Guid userId, PersonalNotificationListQuery query);

    Task<PersonalNotificationDetailResponse?> GetBroadcastByIdAsync(Guid userId, int notificationId);
}
