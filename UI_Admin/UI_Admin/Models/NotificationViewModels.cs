using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UI_Admin.Models;

public sealed class NotificationListPageViewModel
{
    public List<NotificationListItemViewModel> Items { get; set; } = [];
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    [JsonPropertyName("pageNumber")]
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    [JsonPropertyName("totalItems")]
    public int TotalCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NotificationListItemViewModel
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationDetailViewModel : NotificationListItemViewModel
{
    public string Message { get; set; } = string.Empty;
    public int? TargetRegionId { get; set; }
    public int? TargetBranchId { get; set; }
    public List<string> TargetRoles { get; set; } = [];
    public string CreatedByName { get; set; } = string.Empty;
    public int InAppRecipientCount { get; set; }
}

public sealed class SendNotificationViewModel
{
    [Required, StringLength(200)]
    [Display(Name = "Notification title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Target { get; set; } = "Custom";

    [MinLength(1, ErrorMessage = "Select at least one audience role.")]
    public List<string> TargetRoles { get; set; } = [];

    public int? RegionId { get; set; }
    public int? BranchId { get; set; }

    [MinLength(1, ErrorMessage = "Select at least one delivery channel.")]
    public List<string> Channels { get; set; } = [];

    public List<NotificationAudienceRegionOptionViewModel> Regions { get; set; } = [];
    public List<NotificationAudienceBranchOptionViewModel> Branches { get; set; } = [];

    public BroadcastResultViewModel? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class NotificationAudienceOptionsViewModel
{
    public List<NotificationAudienceRegionOptionViewModel> Regions { get; set; } = [];
    public List<NotificationAudienceBranchOptionViewModel> Branches { get; set; } = [];
}

public sealed class NotificationAudienceRegionOptionViewModel
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
}

public sealed class NotificationAudienceBranchOptionViewModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int RegionId { get; set; }
}

public sealed class BroadcastResultViewModel
{
    public int NotificationId { get; set; }
    public int RecipientCount { get; set; }
    public int InAppRecipientCount { get; set; }
    public int EmailSentCount { get; set; }
    public int EmailFailedCount { get; set; }
}
