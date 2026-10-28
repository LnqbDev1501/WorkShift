using System.Text.Json.Serialization;

namespace WorkShiftManagement.BLL.DTOs.Notification.Response;

public sealed class PersonalNotificationListResponse
{
    public int UserNotificationId { get; set; }
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    [JsonPropertyName("pageNumber")]
    public int Page { get; set; }

    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalCount { get; set; }
}

public sealed class PersonalNotificationDetailResponse
{
    public int UserNotificationId { get; set; }
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TargetRegionId { get; set; }
    public int? TargetBranchId { get; set; }
    public List<string> TargetRoles { get; set; } = [];
    public string CreatedByName { get; set; } = string.Empty;
    public int InAppRecipientCount { get; set; }
}

public sealed class BroadcastNotificationListResponse
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
