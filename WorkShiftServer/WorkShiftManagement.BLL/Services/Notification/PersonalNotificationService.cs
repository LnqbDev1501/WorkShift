using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Notification.Request;
using WorkShiftManagement.BLL.DTOs.Notification.Response;
using WorkShiftManagement.BLL.Interfaces.Notification;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Notification;

namespace WorkShiftManagement.BLL.Services.Notification;

public class PersonalNotificationService : IPersonalNotificationService
{
    private readonly IPersonalNotificationRepo _repo;

    public PersonalNotificationService(IPersonalNotificationRepo repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<PersonalNotificationListResponse>> GetMyNotificationsAsync(Guid userId, PersonalNotificationListQuery query)
    {
        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        var (items, totalCount) = await _repo.GetUserNotificationsAsync(
            userId,
            query.Search,
            query.Filter,
            null,
            null,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortOrder);

        return new PagedResponse<PersonalNotificationListResponse>
        {
            Items = items.Select(ToListResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PersonalNotificationDetailResponse?> GetMyNotificationByIdAsync(Guid userId, int userNotificationId)
    {
        var userNotification = await _repo.GetUserNotificationByIdAsync(userId, userNotificationId);
        if (userNotification == null)
        {
            return null;
        }

        var recipientCount = await _repo.CountRecipientsAsync(userNotification.NotificationId);
        return ToDetailResponse(userNotification, recipientCount);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, int userNotificationId)
    {
        return await _repo.MarkAsReadAsync(userId, userNotificationId);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        return await _repo.MarkAllAsReadAsync(userId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _repo.GetUnreadCountAsync(userId);
    }

    private static PersonalNotificationListResponse ToListResponse(UserNotification userNotification)
    {
        var notification = userNotification.Notification;
        return new PersonalNotificationListResponse
        {
            UserNotificationId = userNotification.UserNotificationId,
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels ?? [],
            CreatedAt = userNotification.CreatedAt,
            IsRead = userNotification.IsRead,
            ReadAt = userNotification.ReadAt
        };
    }

    private static PersonalNotificationDetailResponse ToDetailResponse(UserNotification userNotification, int recipientCount)
    {
        var notification = userNotification.Notification;
        return new PersonalNotificationDetailResponse
        {
            UserNotificationId = userNotification.UserNotificationId,
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels ?? [],
            CreatedAt = userNotification.CreatedAt,
            IsRead = userNotification.IsRead,
            ReadAt = userNotification.ReadAt,
            Message = notification.Content,
            TargetRegionId = notification.TargetRegionId,
            TargetBranchId = notification.TargetBranchId,
            TargetRoles = notification.TargetRoles ?? [],
            CreatedByName = notification.CreatedByNavigation?.FullName ?? string.Empty,
            InAppRecipientCount = recipientCount
        };
    }

    private static string GetAudience(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        return notification.TargetType switch
        {
            "All" => "All Users",
            "Staff" => "All Staff",
            "Manager" => "All Managers",
            "Accountant" => "All Accountants",
            "Region" => $"Specific Region: {notification.TargetRegion?.RegionName}",
            "Branch" => $"Specific Branch: {notification.TargetBranch?.BranchName}",
            "Custom" => GetCustomAudience(notification),
            _ => string.IsNullOrWhiteSpace(notification.TargetType) ? "System" : notification.TargetType
        };
    }

    private static string GetCustomAudience(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        if (notification.TargetRoles == null || notification.TargetRoles.Count == 0)
        {
            return "Custom audience";
        }

        var roles = string.Join(", ", notification.TargetRoles.Select(role =>
            char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant()));
        var filters = new List<string> { roles };
        if (notification.TargetRegion != null) filters.Add($"Region: {notification.TargetRegion.RegionName}");
        if (notification.TargetBranch != null) filters.Add($"Branch: {notification.TargetBranch.BranchName}");
        return string.Join(" | ", filters);
    }
}

public class BroadcastNotificationService : IBroadcastNotificationService
{
    private readonly IPersonalNotificationRepo _repo;

    public BroadcastNotificationService(IPersonalNotificationRepo repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<BroadcastNotificationListResponse>> GetBroadcastHistoryAsync(Guid userId, PersonalNotificationListQuery query)
    {
        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        var (items, totalCount) = await _repo.GetBroadcastHistoryAsync(
            userId,
            query.Search,
            query.Filter,
            null,
            null,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortOrder);

        return new PagedResponse<BroadcastNotificationListResponse>
        {
            Items = items.Select(ToBroadcastListResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    private static BroadcastNotificationListResponse ToBroadcastListResponse(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        var userNotification = notification.UserNotifications.FirstOrDefault(un => !un.IsDeleted);
        return new BroadcastNotificationListResponse
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels ?? [],
            CreatedAt = notification.CreatedAt,
            IsRead = userNotification?.IsRead ?? false,
            ReadAt = userNotification?.ReadAt
        };
    }

    public async Task<PersonalNotificationDetailResponse?> GetBroadcastByIdAsync(Guid userId, int notificationId)
    {
        var notification = await _repo.GetBroadcastByIdAsync(userId, notificationId);
        if (notification == null)
        {
            return null;
        }

        var userNotification = notification.UserNotifications.FirstOrDefault(un => !un.IsDeleted);
        var isRead = userNotification?.IsRead ?? false;
        var readAt = userNotification?.ReadAt;

        return new PersonalNotificationDetailResponse
        {
            UserNotificationId = userNotification?.UserNotificationId ?? 0,
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels ?? [],
            CreatedAt = notification.CreatedAt,
            IsRead = isRead,
            ReadAt = readAt,
            Message = notification.Content,
            TargetRegionId = notification.TargetRegionId,
            TargetBranchId = notification.TargetBranchId,
            TargetRoles = notification.TargetRoles ?? [],
            CreatedByName = notification.CreatedByNavigation?.FullName ?? string.Empty,
            InAppRecipientCount = notification.UserNotifications.Count(un => !un.IsDeleted)
        };
    }

    private static string GetAudience(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        return notification.TargetType switch
        {
            "All" => "All Users",
            "Staff" => "All Staff",
            "Manager" => "All Managers",
            "Accountant" => "All Accountants",
            "Region" => $"Specific Region: {notification.TargetRegion?.RegionName}",
            "Branch" => $"Specific Branch: {notification.TargetBranch?.BranchName}",
            "Custom" => GetCustomAudience(notification),
            _ => string.IsNullOrWhiteSpace(notification.TargetType) ? "System" : notification.TargetType
        };
    }

    private static string GetCustomAudience(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        if (notification.TargetRoles == null || notification.TargetRoles.Count == 0)
        {
            return "Custom audience";
        }

        var roles = string.Join(", ", notification.TargetRoles.Select(role =>
            char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant()));
        var filters = new List<string> { roles };
        if (notification.TargetRegion != null) filters.Add($"Region: {notification.TargetRegion.RegionName}");
        if (notification.TargetBranch != null) filters.Add($"Branch: {notification.TargetBranch.BranchName}");
        return string.Join(" | ", filters);
    }
}