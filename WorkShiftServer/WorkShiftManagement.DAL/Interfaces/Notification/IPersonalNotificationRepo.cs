using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Notification;

public interface IPersonalNotificationRepo
{
    Task<(List<UserNotification> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid userId,
        string? search,
        string? filter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder);

    Task<UserNotification?> GetUserNotificationByIdAsync(Guid userId, int userNotificationId);

    Task<int> CountRecipientsAsync(int notificationId);

    Task<bool> MarkAsReadAsync(Guid userId, int userNotificationId);

    Task<int> GetUnreadCountAsync(Guid userId);

    Task<(List<WorkShiftManagement.DAL.Entities.Notification> Items, int TotalCount)> GetBroadcastHistoryAsync(
        Guid userId,
        string? search,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder);

    Task<WorkShiftManagement.DAL.Entities.Notification?> GetBroadcastByIdAsync(Guid userId, int notificationId);
}