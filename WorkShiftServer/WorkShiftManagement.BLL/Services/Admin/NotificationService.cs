using Microsoft.Extensions.Configuration;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class NotificationService : INotificationService
{
    private static readonly HashSet<string> ValidTargets = ["All", "Staff", "Manager", "Accountant", "Region", "Branch"];
    private static readonly HashSet<string> ValidChannels = ["InApp", "Email"];

    private readonly INotificationRepo _notificationRepo;
    private readonly INotificationEmailSender _emailSender;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly string? _createdByEmail;

    public NotificationService(
        INotificationRepo notificationRepo,
        INotificationEmailSender emailSender,
        INotificationRealtimePublisher realtimePublisher,
        IConfiguration configuration)
    {
        _notificationRepo = notificationRepo;
        _emailSender = emailSender;
        _realtimePublisher = realtimePublisher;
        _createdByEmail = configuration["AdminNotification:CreatedByEmail"];
    }

    public async Task<PagedResponse<NotificationListResponse>> GetPagedAsync(NotificationListQuery query)
    {
        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
        {
            throw new ArgumentException("DateFrom must be earlier than or equal to DateTo.");
        }

        var sortBy = NormalizeSortBy(query.SortBy);
        var sortOrder = NormalizeSortOrder(query.SortOrder);
        var (items, totalCount) = await _notificationRepo.GetPagedAsync(
            query.Search,
            query.Type,
            query.DateFrom,
            query.DateTo,
            query.Page,
            query.PageSize,
            sortBy,
            sortOrder);

        return new PagedResponse<NotificationListResponse>
        {
            Items = items.Select(ToListResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<NotificationDetailResponse?> GetByIdAsync(int notificationId)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId);
        if (notification == null)
        {
            return null;
        }

        return new NotificationDetailResponse
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels,
            CreatedAt = notification.CreatedAt,
            Message = notification.Content,
            TargetRegionId = notification.TargetRegionId,
            TargetBranchId = notification.TargetBranchId,
            CreatedByName = notification.CreatedByNavigation.FullName,
            InAppRecipientCount = notification.UserNotifications.Count(userNotification => !userNotification.IsDeleted)
        };
    }

    public async Task<BroadcastNotificationResponse> BroadcastAsync(BroadcastNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message must not be empty or whitespace.");
        }

        var target = NormalizeTarget(request.Target);
        var channels = NormalizeChannels(request.Channels);
        ValidateTargetScope(target, request.RegionId, request.BranchId);
        await ValidateTargetIsActiveAsync(target, request.RegionId, request.BranchId);

        if (string.IsNullOrWhiteSpace(_createdByEmail))
        {
            throw new InvalidOperationException("AdminNotification:CreatedByEmail is not configured.");
        }

        var createdBy = await _notificationRepo.GetActiveAdminByEmailAsync(_createdByEmail);
        if (createdBy == null)
        {
            throw new InvalidOperationException("The configured notification admin was not found or is not active.");
        }

        var recipients = await _notificationRepo.GetRecipientsAsync(target, request.RegionId, request.BranchId);
        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            Title = request.Title.Trim(),
            Content = request.Message.Trim(),
            NotificationType = "Broadcast",
            CreatedBy = createdBy.UserId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = target,
            TargetRegionId = request.RegionId,
            TargetBranchId = request.BranchId,
            Channels = channels
        };

        var hasInAppChannel = channels.Contains("InApp");
        var hasEmailChannel = channels.Contains("Email");
        await _notificationRepo.CreateAsync(notification, recipients.Select(user => user.UserId).ToList(), hasInAppChannel);

        var emailSentCount = 0;
        var emailFailedCount = 0;
        if (hasEmailChannel)
        {
            foreach (var recipient in recipients)
            {
                try
                {
                    await _emailSender.SendAsync(recipient.Email, notification.Title, notification.Content);
                    emailSentCount++;
                }
                catch
                {
                    emailFailedCount++;
                }
            }
        }

        if (hasInAppChannel)
        {
            var realtimeNotification = new RealtimeNotificationResponse
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Message = notification.Content,
                CreatedAt = notification.CreatedAt
            };

            foreach (var recipient in recipients)
            {
                try
                {
                    await _realtimePublisher.PublishAsync(recipient.UserId, realtimeNotification);
                }
                catch
                {
                    // A disconnected SignalR client must not fail a persisted broadcast.
                }
            }
        }

        return new BroadcastNotificationResponse
        {
            NotificationId = notification.NotificationId,
            RecipientCount = recipients.Count,
            InAppRecipientCount = hasInAppChannel ? recipients.Count : 0,
            EmailSentCount = emailSentCount,
            EmailFailedCount = emailFailedCount
        };
    }

    private static string NormalizeTarget(string target)
    {
        var normalizedTarget = ValidTargets.FirstOrDefault(value => string.Equals(value, target?.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalizedTarget ?? throw new ArgumentException("Target must be All, Staff, Manager, Accountant, Region, or Branch.");
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        if (string.Equals(sortBy, "CreatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return "CreatedAt";
        }

        if (string.Equals(sortBy, "Title", StringComparison.OrdinalIgnoreCase))
        {
            return "Title";
        }

        throw new ArgumentException("SortBy must be CreatedAt or Title.");
    }

    private static string NormalizeSortOrder(string? sortOrder)
    {
        if (string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase))
        {
            return "asc";
        }

        if (string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return "desc";
        }

        throw new ArgumentException("SortOrder must be asc or desc.");
    }

    private static List<string> NormalizeChannels(IEnumerable<string>? channels)
    {
        var normalizedChannels = channels?
            .Select(channel => ValidChannels.FirstOrDefault(value => string.Equals(value, channel?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList() ?? [];

        if (normalizedChannels.Count == 0 || normalizedChannels.Any(channel => channel == null))
        {
            throw new ArgumentException("Channels must contain InApp, Email, or both.");
        }

        return normalizedChannels.Select(channel => channel!).Distinct().ToList();
    }

    private static void ValidateTargetScope(string target, int? regionId, int? branchId)
    {
        var isRegionTarget = target == "Region";
        var isBranchTarget = target == "Branch";

        if ((isRegionTarget && (!regionId.HasValue || branchId.HasValue)) ||
            (isBranchTarget && (!branchId.HasValue || regionId.HasValue)) ||
            (!isRegionTarget && !isBranchTarget && (regionId.HasValue || branchId.HasValue)))
        {
            throw new ArgumentException("Region and Branch targets require exactly their matching ID; other targets must not include them.");
        }
    }

    private async Task ValidateTargetIsActiveAsync(string target, int? regionId, int? branchId)
    {
        if (target == "Region" && !await _notificationRepo.IsActiveRegionAsync(regionId!.Value))
        {
            throw new ArgumentException("Region was not found or is inactive.");
        }

        if (target == "Branch" && !await _notificationRepo.IsActiveBranchAsync(branchId!.Value))
        {
            throw new ArgumentException("Branch was not found, is inactive, or belongs to an inactive region.");
        }
    }

    private static NotificationListResponse ToListResponse(Notification notification)
    {
        return new NotificationListResponse
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels,
            CreatedAt = notification.CreatedAt
        };
    }

    private static string GetAudience(Notification notification)
    {
        return notification.TargetType switch
        {
            "All" => "All Users",
            "Staff" => "All Staff",
            "Manager" => "All Managers",
            "Accountant" => "All Accountants",
            "Region" => $"Specific Region: {notification.TargetRegion?.RegionName}",
            "Branch" => $"Specific Branch: {notification.TargetBranch?.BranchName}",
            _ => notification.TargetType
        };
    }
}
