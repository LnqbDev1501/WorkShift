using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class AttendanceRuleService : IAttendanceRuleService
{
    private const int AttendanceRulePageSize = 20;
    private const string StatusDraft = "DRAFT";
    private const string StatusScheduled = "SCHEDULED";
    private const string StatusActive = "ACTIVE";
    private const string StatusInactive = "INACTIVE";
    private const string StatusCancelled = "CANCELLED";

    private readonly IAttendanceRuleRepo _attendanceRuleRepo;
    private readonly INotificationRepo _notificationRepo;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly ILogger<AttendanceRuleService> _logger;

    public AttendanceRuleService(
        IAttendanceRuleRepo attendanceRuleRepo,
        INotificationRepo notificationRepo,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<AttendanceRuleService> logger)
    {
        _attendanceRuleRepo = attendanceRuleRepo;
        _notificationRepo = notificationRepo;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task<AttendanceRuleListResponse> GetAttendanceRulesAsync(GetAttendanceRulesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber <= 0)
        {
            throw new ArgumentException("PageNumber must be greater than 0.");
        }

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();

        if (status is not null and not (StatusDraft or StatusScheduled or StatusActive or StatusInactive or StatusCancelled))
        {
            throw new ArgumentException("Status must be DRAFT, SCHEDULED, ACTIVE, INACTIVE, or CANCELLED.");
        }

        var (rules, totalItems) = await _attendanceRuleRepo.GetPagedAsync(
            request.PageNumber,
            AttendanceRulePageSize,
            search,
            status,
            cancellationToken);

        return new AttendanceRuleListResponse
        {
            Items = rules.Select(Map).ToList(),
            PageNumber = request.PageNumber,
            PageSize = AttendanceRulePageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)AttendanceRulePageSize)
        };
    }

    public async Task<AttendanceRuleResponse> CreateAsync(CreateAttendanceRuleRequest request, Guid createdBy, CancellationToken cancellationToken)
    {
        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator is required.");
        }

        ValidateCreateRequest(request);

        var normalizedName = request.RuleName.Trim();
        var exists = await _attendanceRuleRepo.ExistsByNameAsync(normalizedName, null, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"An attendance rule with the name '{normalizedName}' already exists.");
        }

        var rule = new AttendanceRule
        {
            RuleName = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CheckInBeforeMinutes = request.CheckInBeforeMinutes!.Value,
            CheckInGraceMinutes = request.CheckInGraceMinutes!.Value,
            CheckOutGraceMinutes = request.CheckOutGraceMinutes!.Value,
            CheckOutAfterMinutes = request.CheckOutAfterMinutes!.Value,
            MaxAttendanceDistanceMeters = request.MaxAttendanceDistanceMeters!.Value,
            EffectiveFrom = request.EffectiveFrom!.Value,
            EffectiveTo = null,
            Status = StatusDraft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _attendanceRuleRepo.CreateAsync(rule, cancellationToken);
        return Map(created);
    }

    public async Task<AttendanceRuleResponse> UpdateAsync(int id, UpdateAttendanceRuleRequest request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        ValidateUpdateRequest(request);

        var rule = await _attendanceRuleRepo.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance rule with ID {id} was not found.");

        if (!string.Equals(rule.Status, StatusDraft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only attendance rules in DRAFT status can be updated.");
        }

        var normalizedName = request.RuleName.Trim();
        var exists = await _attendanceRuleRepo.ExistsByNameAsync(normalizedName, id, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"An attendance rule with the name '{normalizedName}' already exists.");
        }

        rule.RuleName = normalizedName;
        rule.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        rule.CheckInBeforeMinutes = request.CheckInBeforeMinutes!.Value;
        rule.CheckInGraceMinutes = request.CheckInGraceMinutes!.Value;
        rule.CheckOutGraceMinutes = request.CheckOutGraceMinutes!.Value;
        rule.CheckOutAfterMinutes = request.CheckOutAfterMinutes!.Value;
        rule.MaxAttendanceDistanceMeters = request.MaxAttendanceDistanceMeters!.Value;
        rule.EffectiveFrom = request.EffectiveFrom!.Value;
        rule.UpdatedAt = DateTime.UtcNow;

        var updated = await _attendanceRuleRepo.UpdateAsync(rule, cancellationToken);
        return Map(updated);
    }

    public async Task<AttendanceRuleResponse> PublishAsync(int id, Guid adminUserId, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator is required.");
        }

        var rule = await _attendanceRuleRepo.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance rule with ID {id} was not found.");

        if (!string.Equals(rule.Status, StatusDraft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"This action requires DRAFT status. Current status: {rule.Status}.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (!rule.EffectiveFrom.HasValue || rule.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException($"The effective date ({(rule.EffectiveFrom.HasValue ? rule.EffectiveFrom.Value.ToString("yyyy-MM-dd") : "N/A")}) has already passed or is today. Please update EffectiveFrom to a future date before publishing.");
        }

        var existingScheduled = await _attendanceRuleRepo.GetScheduledRuleAsync(cancellationToken);
        if (existingScheduled != null && existingScheduled.AttendanceRuleId != id)
        {
            throw new InvalidOperationException($"An attendance rule '{existingScheduled.RuleName}' is already scheduled (effective from {existingScheduled.EffectiveFrom:yyyy-MM-dd}). Cancel that rule before publishing another.");
        }

        var currentActive = await _attendanceRuleRepo.GetActiveRuleAsync(cancellationToken);
        if (currentActive != null && currentActive.EffectiveFrom >= rule.EffectiveFrom.Value)
        {
            throw new InvalidOperationException("The effective date must be after the current active version.");
        }

        rule.Status = StatusScheduled;
        rule.PublishedBy = adminUserId;
        rule.PublishedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        var updated = await _attendanceRuleRepo.UpdateAsync(rule, cancellationToken);

        await SendPublishNotificationAsync(rule, adminUserId);

        return Map(updated);
    }

    public async Task ActivateDueAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var dueScheduledRules = await _attendanceRuleRepo.GetDueScheduledRulesAsync(today, cancellationToken);
        if (dueScheduledRules.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var nextRule in dueScheduledRules)
        {
            var activeRules = await _attendanceRuleRepo.GetActiveRulesAsync(cancellationToken);
            foreach (var activeRule in activeRules)
            {
                if (activeRule.AttendanceRuleId == nextRule.AttendanceRuleId) continue;
                activeRule.Status = StatusInactive;
                activeRule.EffectiveTo = nextRule.EffectiveFrom;
                activeRule.UpdatedAt = now;
            }

            nextRule.Status = StatusActive;
            nextRule.EffectiveTo = null;
            nextRule.UpdatedAt = now;

            await _attendanceRuleRepo.SaveChangesAsync(cancellationToken);

            await SendActiveNotificationAsync(nextRule);
        }
    }

    public async Task<AttendanceRuleResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        var rule = await _attendanceRuleRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance rule with ID {id} was not found.");
        return Map(rule);
    }

    public async Task<List<AttendanceRuleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rules = await _attendanceRuleRepo.GetAllAsync(cancellationToken);
        return rules.Select(Map).ToList();
    }

    private async Task SendPublishNotificationAsync(AttendanceRule rule, Guid adminUserId)
    {
        try
        {
            var targetRoles = new List<string> { "STAFF", "MANAGER", "ACCOUNTANT" };
            var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, null, null);

            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Title = $"New Attendance Rule Published: {rule.RuleName}",
                Content = $"A new attendance rule '{rule.RuleName}' has been published and will take effect on {rule.EffectiveFrom:yyyy-MM-dd}. Details: Check-in before {rule.CheckInBeforeMinutes} mins, check-in grace {rule.CheckInGraceMinutes} mins, check-out grace {rule.CheckOutGraceMinutes} mins, check-out after {rule.CheckOutAfterMinutes} mins, max distance {rule.MaxAttendanceDistanceMeters}m.",
                NotificationType = "Broadcast",
                CreatedBy = adminUserId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                TargetRoles = targetRoles,
                Channels = new List<string> { "InApp" }
            };

            var recipientUserIds = recipients.Select(u => u.UserId).ToList();
            await _notificationRepo.CreateAsync(notification, recipientUserIds, true);

            var realtime = new RealtimeNotificationResponse
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
                    await _realtimePublisher.PublishAsync(recipient.UserId, realtime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish realtime notification to user {UserId}", recipient.UserId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send in-app broadcast notification for attendance rule {RuleId}", rule.AttendanceRuleId);
        }
    }

    private async Task SendActiveNotificationAsync(AttendanceRule rule)
    {
        try
        {
            var targetRoles = new List<string> { "STAFF", "MANAGER", "ACCOUNTANT" };
            var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, null, null);

            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Title = $"Attendance Rule '{rule.RuleName}' is Now Active",
                Content = $"Attendance rule '{rule.RuleName}' has officially taken effect starting today ({rule.EffectiveFrom:yyyy-MM-dd}).",
                NotificationType = "Broadcast",
                CreatedBy = rule.CreatedBy,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Custom",
                TargetRoles = targetRoles,
                Channels = new List<string> { "InApp" }
            };

            var recipientUserIds = recipients.Select(u => u.UserId).ToList();
            await _notificationRepo.CreateAsync(notification, recipientUserIds, true);

            var realtime = new RealtimeNotificationResponse
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
                    await _realtimePublisher.PublishAsync(recipient.UserId, realtime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish realtime notification to user {UserId}", recipient.UserId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send in-app active notification for attendance rule {RuleId}", rule.AttendanceRuleId);
        }
    }

    private static void ValidateCreateRequest(CreateAttendanceRuleRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Request payload is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RuleName))
        {
            throw new ArgumentException("ruleName must be a valid text string.");
        }

        if (request.RuleName.Trim().Length > 150)
        {
            throw new ArgumentException("ruleName cannot exceed 150 characters.");
        }

        // 1. CheckInBeforeMinutes
        if (!request.CheckInBeforeMinutes.HasValue || request.CheckInBeforeMinutes.Value < 0 || request.CheckInBeforeMinutes.Value > 1440)
        {
            throw new ArgumentException("checkInBeforeMinutes must be a valid integer number between 0 and 1440.");
        }

        // 2. CheckInGraceMinutes
        if (!request.CheckInGraceMinutes.HasValue || request.CheckInGraceMinutes.Value < 0 || request.CheckInGraceMinutes.Value > 1440)
        {
            throw new ArgumentException("checkInGraceMinutes must be a valid integer number between 0 and 1440.");
        }

        // 3. CheckOutGraceMinutes
        if (!request.CheckOutGraceMinutes.HasValue || request.CheckOutGraceMinutes.Value < 0 || request.CheckOutGraceMinutes.Value > 1440)
        {
            throw new ArgumentException("checkOutGraceMinutes must be a valid integer number between 0 and 1440.");
        }

        // 4. CheckOutAfterMinutes
        if (!request.CheckOutAfterMinutes.HasValue || request.CheckOutAfterMinutes.Value < 0 || request.CheckOutAfterMinutes.Value > 1440)
        {
            throw new ArgumentException("checkOutAfterMinutes must be a valid integer number between 0 and 1440.");
        }

        // 5. MaxAttendanceDistanceMeters
        if (!request.MaxAttendanceDistanceMeters.HasValue || request.MaxAttendanceDistanceMeters.Value <= 0 || request.MaxAttendanceDistanceMeters.Value > 100000)
        {
            throw new ArgumentException("maxAttendanceDistanceMeters must be a valid integer number greater than 0.");
        }

        // 6. EffectiveFrom
        if (!request.EffectiveFrom.HasValue)
        {
            throw new ArgumentException("effectiveFrom must be a valid date in YYYY-MM-DD format (e.g. '2026-10-01').");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException("Effective From date must be in the future (after today).");
        }
    }

    private static void ValidateUpdateRequest(UpdateAttendanceRuleRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Request payload is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RuleName))
        {
            throw new ArgumentException("ruleName must be a valid text string.");
        }

        if (request.RuleName.Trim().Length > 150)
        {
            throw new ArgumentException("ruleName cannot exceed 150 characters.");
        }

        // 1. CheckInBeforeMinutes
        if (!request.CheckInBeforeMinutes.HasValue || request.CheckInBeforeMinutes.Value < 0 || request.CheckInBeforeMinutes.Value > 1440)
        {
            throw new ArgumentException("checkInBeforeMinutes must be a valid integer number between 0 and 1440.");
        }

        // 2. CheckInGraceMinutes
        if (!request.CheckInGraceMinutes.HasValue || request.CheckInGraceMinutes.Value < 0 || request.CheckInGraceMinutes.Value > 1440)
        {
            throw new ArgumentException("checkInGraceMinutes must be a valid integer number between 0 and 1440.");
        }

        // 3. CheckOutGraceMinutes
        if (!request.CheckOutGraceMinutes.HasValue || request.CheckOutGraceMinutes.Value < 0 || request.CheckOutGraceMinutes.Value > 1440)
        {
            throw new ArgumentException("checkOutGraceMinutes must be a valid integer number between 0 and 1440.");
        }

        // 4. CheckOutAfterMinutes
        if (!request.CheckOutAfterMinutes.HasValue || request.CheckOutAfterMinutes.Value < 0 || request.CheckOutAfterMinutes.Value > 1440)
        {
            throw new ArgumentException("checkOutAfterMinutes must be a valid integer number between 0 and 1440.");
        }

        // 5. MaxAttendanceDistanceMeters
        if (!request.MaxAttendanceDistanceMeters.HasValue || request.MaxAttendanceDistanceMeters.Value <= 0 || request.MaxAttendanceDistanceMeters.Value > 100000)
        {
            throw new ArgumentException("maxAttendanceDistanceMeters must be a valid integer number greater than 0.");
        }

        // 6. EffectiveFrom
        if (!request.EffectiveFrom.HasValue)
        {
            throw new ArgumentException("effectiveFrom must be a valid date in YYYY-MM-DD format (e.g. '2026-10-01').");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException("Effective From date must be in the future (after today).");
        }
    }

    private static AttendanceRuleResponse Map(AttendanceRule rule) => new()
    {
        AttendanceRuleId = rule.AttendanceRuleId,
        RuleName = rule.RuleName,
        Description = rule.Description,
        CheckInBeforeMinutes = rule.CheckInBeforeMinutes,
        CheckInGraceMinutes = rule.CheckInGraceMinutes,
        CheckOutGraceMinutes = rule.CheckOutGraceMinutes,
        CheckOutAfterMinutes = rule.CheckOutAfterMinutes,
        MaxAttendanceDistanceMeters = rule.MaxAttendanceDistanceMeters,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo,
        Status = rule.Status,
        CreatedBy = rule.CreatedBy,
        CreatedByName = rule.CreatedByNavigation?.FullName ?? rule.CreatedByNavigation?.Email,
        PublishedBy = rule.PublishedBy,
        PublishedAt = rule.PublishedAt,
        CancelledBy = rule.CancelledBy,
        CancelledAt = rule.CancelledAt,
        CancellationReason = rule.CancellationReason,
        CreatedAt = rule.CreatedAt,
        UpdatedAt = rule.UpdatedAt
    };
}
