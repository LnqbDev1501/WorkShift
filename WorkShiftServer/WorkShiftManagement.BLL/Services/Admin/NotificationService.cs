using Microsoft.Extensions.Configuration;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class NotificationService : INotificationService
{
    private static readonly HashSet<string> ValidTargets = ["Custom"];
    private static readonly HashSet<string> ValidRoles = ["STAFF", "MANAGER", "ACCOUNTANT"];
    private static readonly HashSet<string> ValidChannels = ["InApp", "Email"];

    private readonly INotificationRepo _notificationRepo;
    private readonly INotificationEmailSender _emailSender;
    private readonly INotificationRealtimePublisher _realtimePublisher;

    public NotificationService(
        INotificationRepo notificationRepo,
        INotificationEmailSender emailSender,
        INotificationRealtimePublisher realtimePublisher)
    {
        _notificationRepo = notificationRepo;
        _emailSender = emailSender;
        _realtimePublisher = realtimePublisher;
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
            Channels = notification.Channels ?? [],
            CreatedAt = notification.CreatedAt,
            Message = notification.Content,
            TargetRegionId = notification.TargetRegionId,
            TargetBranchId = notification.TargetBranchId,
            TargetRoles = notification.TargetRoles ?? [],
            CreatedByName = notification.CreatedByNavigation?.FullName ?? string.Empty,
            InAppRecipientCount = notification.UserNotifications.Count(userNotification => !userNotification.IsDeleted)
        };
    }

    public async Task<BroadcastNotificationResponse> BroadcastAsync(BroadcastNotificationRequest request, Guid? senderUserId = null, string? senderEmail = null)
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
        var targetRoles = NormalizeRoles(request.TargetRoles);
        var channels = NormalizeChannels(request.Channels);
        await ValidateTargetIsActiveAsync(request.RegionId, request.BranchId);

        User? createdBy = null;
        if (senderUserId.HasValue && senderUserId.Value != Guid.Empty)
        {
            createdBy = await _notificationRepo.GetActiveAdminByIdAsync(senderUserId.Value);
        }

        if (createdBy == null && !string.IsNullOrWhiteSpace(senderEmail))
        {
            createdBy = await _notificationRepo.GetActiveAdminByEmailAsync(senderEmail);
        }

        if (createdBy == null)
        {
            createdBy = await _notificationRepo.GetAnyActiveAdminAsync();
        }

        if (createdBy == null)
        {
            throw new InvalidOperationException("No active administrator account was found to dispatch the broadcast.");
        }

        var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, request.RegionId, request.BranchId);
        var now = DateTime.UtcNow;
        var notification = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = request.Title.Trim(),
            Content = request.Message.Trim(),
            NotificationType = "Broadcast",
            CreatedBy = createdBy.UserId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            TargetType = target,
            TargetRoles = targetRoles,
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
        return normalizedTarget ?? throw new ArgumentException("Target must be Custom.");
    }

    private static List<string> NormalizeRoles(IEnumerable<string>? roles)
    {
        var normalizedRoles = roles?
            .Select(role => ValidRoles.FirstOrDefault(value => string.Equals(value, role?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList() ?? [];

        if (normalizedRoles.Count == 0 || normalizedRoles.Any(role => role == null))
        {
            throw new ArgumentException("TargetRoles must contain at least one of Staff, Manager, or Accountant.");
        }

        return normalizedRoles.Select(role => role!).Distinct().ToList();
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

    private async Task ValidateTargetIsActiveAsync(int? regionId, int? branchId)
    {
        if (regionId.HasValue && !await _notificationRepo.IsActiveRegionAsync(regionId.Value))
        {
            throw new ArgumentException("Region was not found or is inactive.");
        }

        if (branchId.HasValue && !await _notificationRepo.IsActiveBranchAsync(branchId.Value))
        {
            throw new ArgumentException("Branch was not found, is inactive, or belongs to an inactive region.");
        }
    }

    private static NotificationListResponse ToListResponse(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        return new NotificationListResponse
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            NotificationType = notification.NotificationType,
            Audience = GetAudience(notification),
            Channels = notification.Channels ?? [],
            CreatedAt = notification.CreatedAt
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

    public async Task<NotificationAudienceOptionsResponse> GetAudienceOptionsAsync()
    {
        var options = await _notificationRepo.GetAudienceOptionsAsync();
        return new NotificationAudienceOptionsResponse
        {
            Regions = options.Regions.Select(region => new NotificationAudienceRegionOptionResponse { RegionId = region.RegionId, RegionName = region.RegionName }).ToList(),
            Branches = options.Branches.Select(branch => new NotificationAudienceBranchOptionResponse { BranchId = branch.BranchId, BranchName = branch.BranchName, RegionId = branch.RegionId }).ToList()
        };
    }

    private static string GetCustomAudience(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        if (notification.TargetRoles == null || notification.TargetRoles.Count == 0)
        {
            return "Custom audience";
        }

        var roles = string.Join(", ", notification.TargetRoles.Select(role => char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant()));
        var filters = new List<string> { roles };
        if (notification.TargetRegion != null) filters.Add($"Region: {notification.TargetRegion.RegionName}");
        if (notification.TargetBranch != null) filters.Add($"Branch: {notification.TargetBranch.BranchName}");
        return string.Join(" | ", filters);
    }
}
