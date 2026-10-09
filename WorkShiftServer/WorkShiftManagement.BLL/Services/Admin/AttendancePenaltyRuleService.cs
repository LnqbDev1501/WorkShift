using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class AttendancePenaltyRuleService : IAttendancePenaltyRuleService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "DRAFT", "SCHEDULED", "ACTIVE", "INACTIVE", "CANCELLED"
    };

    private readonly IAttendancePenaltyRuleRepo _attendancePenaltyRuleRepo;
    private readonly INotificationRepo _notificationRepo;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly IAdminRealtimePublisher _adminRealtimePublisher;
    private readonly ILogger<AttendancePenaltyRuleService> _logger;

    public AttendancePenaltyRuleService(
        IAttendancePenaltyRuleRepo attendancePenaltyRuleRepo,
        INotificationRepo notificationRepo,
        INotificationRealtimePublisher realtimePublisher,
        IAdminRealtimePublisher adminRealtimePublisher,
        ILogger<AttendancePenaltyRuleService> logger)
    {
        _attendancePenaltyRuleRepo = attendancePenaltyRuleRepo;
        _notificationRepo = notificationRepo;
        _realtimePublisher = realtimePublisher;
        _adminRealtimePublisher = adminRealtimePublisher;
        _logger = logger;
    }

    public async Task<AttendancePenaltyRuleListResponse> GetAttendancePenaltyRulesAsync(
        GetAttendancePenaltyRulesRequest request,
        CancellationToken cancellationToken)
    {
        const int pageSize = 20;
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;

        if (!string.IsNullOrWhiteSpace(request.Status) && !AllowedStatuses.Contains(request.Status.Trim()))
        {
            throw new ArgumentException("status must be one of: DRAFT, SCHEDULED, ACTIVE, INACTIVE, CANCELLED.");
        }

        var (items, totalItems) = await _attendancePenaltyRuleRepo.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Search,
            request.Status,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return new AttendancePenaltyRuleListResponse
        {
            Items = items.Select(MapListItem).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<AttendancePenaltyRuleDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        var rule = await _attendancePenaltyRuleRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance penalty rule with ID {id} was not found.");

        return MapDetail(rule);
    }

    public async Task<AttendancePenaltyRuleDetailResponse> CreateAsync(
        CreateAttendancePenaltyRuleRequest request,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator is required.");
        }

        ValidateCreateRequest(request);

        if (await _attendancePenaltyRuleRepo.ExistsByNameAsync(request.RuleName, null, cancellationToken))
        {
            throw new InvalidOperationException($"An attendance penalty rule with the name '{request.RuleName.Trim()}' already exists.");
        }

        var now = DateTime.UtcNow;
        var rule = new AttendancePenaltyRule
        {
            RuleName = request.RuleName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = null,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var level in request.PenaltyLevels!)
        {
            var normalizedType = NormalizeViolationType(level.ViolationType);
            var normalizedAmount = NormalizePenaltyAmount(level.PenaltyAmount);
            rule.AttendancePenaltyLevels.Add(new AttendancePenaltyLevel
            {
                PenaltyRule = rule,
                ViolationType = normalizedType,
                FromMinutes = level.FromMinutes!.Value,
                ToMinutes = level.ToMinutes,
                PenaltyAmount = normalizedAmount,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        var created = await _attendancePenaltyRuleRepo.CreateAsync(rule, cancellationToken);

        return await GetByIdAsync(created.PenaltyRuleId, cancellationToken);
    }

    public async Task<AttendancePenaltyRuleDetailResponse> UpdateAsync(
        int id,
        UpdateAttendancePenaltyRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        ValidateUpdateRequest(request);

        var rule = await _attendancePenaltyRuleRepo.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance penalty rule with ID {id} was not found.");

        if (rule.Status != "DRAFT")
        {
            throw new InvalidOperationException($"Only rules with status 'DRAFT' can be updated. Current status is '{rule.Status}'.");
        }

        if (await _attendancePenaltyRuleRepo.ExistsByNameAsync(request.RuleName, id, cancellationToken))
        {
            throw new InvalidOperationException($"An attendance penalty rule with the name '{request.RuleName.Trim()}' already exists.");
        }

        var now = DateTime.UtcNow;
        rule.RuleName = request.RuleName.Trim();
        rule.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.UpdatedAt = now;

        var newLevels = new List<AttendancePenaltyLevel>();
        foreach (var level in request.PenaltyLevels!)
        {
            var normalizedType = NormalizeViolationType(level.ViolationType);
            var normalizedAmount = NormalizePenaltyAmount(level.PenaltyAmount);
            newLevels.Add(new AttendancePenaltyLevel
            {
                ViolationType = normalizedType,
                FromMinutes = level.FromMinutes!.Value,
                ToMinutes = level.ToMinutes,
                PenaltyAmount = normalizedAmount,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _attendancePenaltyRuleRepo.UpdateDraftAsync(rule, newLevels, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AttendancePenaltyRuleDetailResponse> PublishAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator is required.");
        }

        var rule = await _attendancePenaltyRuleRepo.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance penalty rule with ID {id} was not found.");

        if (!string.Equals(rule.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"This action requires DRAFT status. Current status: {rule.Status}.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (!rule.EffectiveFrom.HasValue || rule.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException($"The effective date ({(rule.EffectiveFrom.HasValue ? rule.EffectiveFrom.Value.ToString("yyyy-MM-dd") : "N/A")}) has already passed or is today. Please update EffectiveFrom to a future date before publishing.");
        }

        if (rule.AttendancePenaltyLevels.Count == 0)
        {
            throw new InvalidOperationException("Cannot publish an attendance penalty rule without any penalty levels.");
        }

        var existingScheduled = await _attendancePenaltyRuleRepo.GetScheduledRuleAsync(cancellationToken);
        if (existingScheduled != null && existingScheduled.PenaltyRuleId != id)
        {
            throw new InvalidOperationException($"An attendance penalty rule '{existingScheduled.RuleName}' is already scheduled (effective from {existingScheduled.EffectiveFrom:yyyy-MM-dd}). Cancel that rule before publishing another.");
        }

        var currentActive = await _attendancePenaltyRuleRepo.GetActiveRuleAsync(cancellationToken);
        if (currentActive != null && currentActive.EffectiveFrom.HasValue && currentActive.EffectiveFrom >= rule.EffectiveFrom.Value)
        {
            throw new InvalidOperationException("The effective date must be after the current active version.");
        }

        var now = DateTime.UtcNow;
        rule.Status = "SCHEDULED";
        rule.PublishedBy = adminUserId;
        rule.PublishedAt = now;
        rule.UpdatedAt = now;

        await _attendancePenaltyRuleRepo.UpdateAsync(rule, cancellationToken);

        await SendPublishNotificationAsync(rule, adminUserId);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task SendPublishNotificationAsync(AttendancePenaltyRule rule, Guid adminUserId)
    {
        try
        {
            var targetRoles = new List<string> { "STAFF", "MANAGER", "ACCOUNTANT" };
            var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, null, null);

            var levelSummaries = rule.AttendancePenaltyLevels
                .OrderBy(l => l.ViolationType)
                .ThenBy(l => l.FromMinutes)
                .Select(l =>
                {
                    var typeName = l.ViolationType == "LATE" ? "Late" : "Early leave";
                    var toText = l.ToMinutes.HasValue ? $"{l.FromMinutes}-{l.ToMinutes.Value} mins" : $">={l.FromMinutes} mins";
                    return $"{typeName} {toText} ({l.PenaltyAmount:N0} VND)";
                });

            var detailsText = levelSummaries.Any()
                ? $" Penalty details: {string.Join(", ", levelSummaries)}."
                : string.Empty;

            var now = DateTime.UtcNow;
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = $"New Attendance Penalty Rule Published: {rule.RuleName}",
                Content = $"A new attendance penalty rule '{rule.RuleName}' has been published and will take effect on {rule.EffectiveFrom:yyyy-MM-dd}.{detailsText}",
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
            _logger.LogError(ex, "Failed to send in-app broadcast notification for attendance penalty rule {RuleId}", rule.PenaltyRuleId);
        }
    }

    public async Task ActivateDueAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var dueScheduledRules = await _attendancePenaltyRuleRepo.GetDueScheduledRulesAsync(today, cancellationToken);
        if (dueScheduledRules.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var nextRule in dueScheduledRules)
        {
            var activeRules = await _attendancePenaltyRuleRepo.GetActiveRulesAsync(cancellationToken);
            foreach (var activeRule in activeRules)
            {
                if (activeRule.PenaltyRuleId == nextRule.PenaltyRuleId) continue;
                activeRule.Status = "INACTIVE";
                activeRule.EffectiveTo = nextRule.EffectiveFrom;
                activeRule.UpdatedAt = now;
            }

            nextRule.Status = "ACTIVE";
            nextRule.EffectiveTo = null;
            nextRule.UpdatedAt = now;

            await _attendancePenaltyRuleRepo.SaveChangesAsync(cancellationToken);

            await SendActiveNotificationAsync(nextRule);
            await _adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new { action = "ACTIVATED" });
        }
    }

    private async Task SendActiveNotificationAsync(AttendancePenaltyRule rule)
    {
        try
        {
            var targetRoles = new List<string> { "STAFF", "MANAGER", "ACCOUNTANT" };
            var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, null, null);

            var now = DateTime.UtcNow;
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = $"Attendance Penalty Rule '{rule.RuleName}' is Now Active",
                Content = $"Attendance penalty rule '{rule.RuleName}' has officially taken effect starting today ({rule.EffectiveFrom:yyyy-MM-dd}).",
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
            _logger.LogError(ex, "Failed to send in-app active notification for attendance penalty rule {RuleId}", rule.PenaltyRuleId);
        }
    }

    public async Task<AttendancePenaltyRuleDetailResponse> CancelAsync(
        int id,
        CancelAttendancePenaltyRuleRequest? request,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentException("id must be a positive integer.");
        }

        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("An authenticated administrator is required.");
        }

        var rule = await _attendancePenaltyRuleRepo.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Attendance penalty rule with ID {id} was not found.");

        if (!string.Equals(rule.Status, "DRAFT", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(rule.Status, "SCHEDULED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only rules with status 'DRAFT' or 'SCHEDULED' can be cancelled. Current status is '{rule.Status}'.");
        }

        var previousStatus = rule.Status;
        var now = DateTime.UtcNow;

        rule.Status = "CANCELLED";
        rule.CancelledBy = adminUserId;
        rule.CancelledAt = now;
        rule.CancellationReason = string.IsNullOrWhiteSpace(request?.CancellationReason) ? null : request.CancellationReason.Trim();
        rule.UpdatedAt = now;

        await _attendancePenaltyRuleRepo.SaveChangesAsync(cancellationToken);

        if (string.Equals(previousStatus, "SCHEDULED", StringComparison.OrdinalIgnoreCase))
        {
            await SendCancelNotificationAsync(rule);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task SendCancelNotificationAsync(AttendancePenaltyRule rule)
    {
        try
        {
            var targetRoles = new List<string> { "STAFF", "MANAGER", "ACCOUNTANT" };
            var recipients = await _notificationRepo.GetRecipientsAsync(targetRoles, null, null);

            var reasonText = !string.IsNullOrWhiteSpace(rule.CancellationReason)
                ? $" Reason: {rule.CancellationReason.Trim()}."
                : string.Empty;

            var effectiveDateText = rule.EffectiveFrom.HasValue
                ? $" (previously planned for {rule.EffectiveFrom.Value:yyyy-MM-dd})"
                : string.Empty;

            var now = DateTime.UtcNow;
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = $"Cancelled: Scheduled Attendance Penalty Rule '{rule.RuleName}'",
                Content = $"The scheduled attendance penalty rule '{rule.RuleName}'{effectiveDateText} has been cancelled by the administration.{reasonText}",
                NotificationType = "Broadcast",
                CreatedBy = rule.CancelledBy ?? rule.CreatedBy,
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
            _logger.LogError(ex, "Failed to send in-app cancel notification for attendance penalty rule {RuleId}", rule.PenaltyRuleId);
        }
    }

    private static decimal NormalizePenaltyAmount(decimal? amount)
    {
        if (!amount.HasValue || amount.Value <= 0)
        {
            throw new ArgumentException("penaltyAmount must be greater than 0.");
        }

        var value = amount.Value;
        if (value < 1000)
        {
            value *= 1000;
        }

        return value;
    }

    private static string NormalizeViolationType(string? violationType)
    {
        var normalized = violationType?.Trim().ToUpperInvariant();
        return normalized switch
        {
            "LATE" => "LATE",
            "EARLY_LEAVE" => "EARLY_LEAVE",
            _ => throw new ArgumentException("violationType must be either 'LATE' or 'EARLY_LEAVE'.")
        };
    }

    private static void ValidateCreateRequest(CreateAttendancePenaltyRuleRequest request)
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

        if (!request.EffectiveFrom.HasValue)
        {
            throw new ArgumentException("effectiveFrom must be a valid date in YYYY-MM-DD format (e.g. '2026-10-01').");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException("Effective From date must be in the future (after today).");
        }

        if (request.PenaltyLevels is null || request.PenaltyLevels.Count == 0)
        {
            throw new ArgumentException("At least one penalty level is required.");
        }

        foreach (var level in request.PenaltyLevels)
        {
            // Validate violation type
            NormalizeViolationType(level.ViolationType);

            if (!level.FromMinutes.HasValue || level.FromMinutes.Value < 1 || level.FromMinutes.Value > 480)
            {
                throw new ArgumentException("fromMinutes must be a valid integer between 1 and 480.");
            }

            if (level.ToMinutes.HasValue && (level.ToMinutes.Value < level.FromMinutes.Value || level.ToMinutes.Value > 480))
            {
                throw new ArgumentException("toMinutes must be greater than or equal to fromMinutes and at most 480, or left null for open-ended intervals.");
            }

            if (!level.PenaltyAmount.HasValue || level.PenaltyAmount.Value <= 0)
            {
                throw new ArgumentException("penaltyAmount must be greater than 0.");
            }
        }

        // Validate continuous intervals per violation type (no gaps, no overlaps, starts at 1, last is open-ended)
        var grouped = request.PenaltyLevels
            .GroupBy(l => NormalizeViolationType(l.ViolationType));

        foreach (var group in grouped)
        {
            var sortedLevels = group.OrderBy(l => l.FromMinutes!.Value).ToList();

            if (sortedLevels[0].FromMinutes!.Value != 1)
            {
                throw new ArgumentException(
                    $"The first penalty tier for {group.Key} must start at minute 1 (found {sortedLevels[0].FromMinutes}).");
            }

            for (var i = 0; i < sortedLevels.Count - 1; i++)
            {
                var current = sortedLevels[i];
                var next = sortedLevels[i + 1];

                if (!current.ToMinutes.HasValue)
                {
                    throw new ArgumentException(
                        $"Penalty tier for {group.Key} starting at {current.FromMinutes} mins has no upper bound and cannot be followed by another tier ({next.FromMinutes} mins).");
                }

                if (current.ToMinutes.Value >= next.FromMinutes!.Value)
                {
                    throw new ArgumentException(
                        $"Penalty tiers for {group.Key} have overlapping minute ranges ({current.FromMinutes}-{current.ToMinutes} and {next.FromMinutes}-{next.ToMinutes}).");
                }

                if (next.FromMinutes.Value != current.ToMinutes.Value + 1)
                {
                    throw new ArgumentException(
                        $"Gap detected in penalty tiers for {group.Key}: tier starting at {next.FromMinutes} mins must start immediately at minute {current.ToMinutes.Value + 1} (right after {current.ToMinutes.Value} mins).");
                }
            }

            var lastLevel = sortedLevels[sortedLevels.Count - 1];
            if (lastLevel.ToMinutes.HasValue && lastLevel.ToMinutes.Value != 480)
            {
                throw new ArgumentException(
                    $"The last penalty tier for {group.Key} (from {lastLevel.FromMinutes} mins) must either reach 480 mins or leave ToMinutes empty to apply for all remaining minutes.");
            }
        }
    }

    private static void ValidateUpdateRequest(UpdateAttendancePenaltyRuleRequest request)
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

        if (!request.EffectiveFrom.HasValue)
        {
            throw new ArgumentException("effectiveFrom must be a valid date in YYYY-MM-DD format (e.g. '2026-10-01').");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.EffectiveFrom.Value <= today)
        {
            throw new ArgumentException("Effective From date must be in the future (after today).");
        }

        if (request.PenaltyLevels is null || request.PenaltyLevels.Count == 0)
        {
            throw new ArgumentException("At least one penalty level is required.");
        }

        foreach (var level in request.PenaltyLevels)
        {
            // Validate violation type
            NormalizeViolationType(level.ViolationType);

            if (!level.FromMinutes.HasValue || level.FromMinutes.Value < 1 || level.FromMinutes.Value > 480)
            {
                throw new ArgumentException("fromMinutes must be a valid integer between 1 and 480.");
            }

            if (level.ToMinutes.HasValue && (level.ToMinutes.Value < level.FromMinutes.Value || level.ToMinutes.Value > 480))
            {
                throw new ArgumentException("toMinutes must be greater than or equal to fromMinutes and at most 480, or left null for open-ended intervals.");
            }

            if (!level.PenaltyAmount.HasValue || level.PenaltyAmount.Value <= 0)
            {
                throw new ArgumentException("penaltyAmount must be greater than 0.");
            }
        }

        // Validate continuous intervals per violation type (no gaps, no overlaps, starts at 1, last is open-ended)
        var grouped = request.PenaltyLevels
            .GroupBy(l => NormalizeViolationType(l.ViolationType));

        foreach (var group in grouped)
        {
            var sortedLevels = group.OrderBy(l => l.FromMinutes!.Value).ToList();

            if (sortedLevels[0].FromMinutes!.Value != 1)
            {
                throw new ArgumentException(
                    $"The first penalty tier for {group.Key} must start at minute 1 (found {sortedLevels[0].FromMinutes}).");
            }

            for (var i = 0; i < sortedLevels.Count - 1; i++)
            {
                var current = sortedLevels[i];
                var next = sortedLevels[i + 1];

                if (!current.ToMinutes.HasValue)
                {
                    throw new ArgumentException(
                        $"Penalty tier for {group.Key} starting at {current.FromMinutes} mins has no upper bound and cannot be followed by another tier ({next.FromMinutes} mins).");
                }

                if (current.ToMinutes.Value >= next.FromMinutes!.Value)
                {
                    throw new ArgumentException(
                        $"Penalty tiers for {group.Key} have overlapping minute ranges ({current.FromMinutes}-{current.ToMinutes} and {next.FromMinutes}-{next.ToMinutes}).");
                }

                if (next.FromMinutes.Value != current.ToMinutes.Value + 1)
                {
                    throw new ArgumentException(
                        $"Gap detected in penalty tiers for {group.Key}: tier starting at {next.FromMinutes} mins must start immediately at minute {current.ToMinutes.Value + 1} (right after {current.ToMinutes.Value} mins).");
                }
            }

            var lastLevel = sortedLevels[sortedLevels.Count - 1];
            if (lastLevel.ToMinutes.HasValue && lastLevel.ToMinutes.Value != 480)
            {
                throw new ArgumentException(
                    $"The last penalty tier for {group.Key} (from {lastLevel.FromMinutes} mins) must either reach 480 mins or leave ToMinutes empty to apply for all remaining minutes.");
            }
        }
    }

    private static AttendancePenaltyRuleItemResponse MapListItem(AttendancePenaltyRule rule) => new()
    {
        PenaltyRuleId = rule.PenaltyRuleId,
        RuleName = rule.RuleName,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo,
        Status = rule.Status,
        CreatedAt = rule.CreatedAt
    };

    private static AttendancePenaltyRuleDetailResponse MapDetail(AttendancePenaltyRule rule) => new()
    {
        PenaltyRuleId = rule.PenaltyRuleId,
        RuleName = rule.RuleName,
        Description = rule.Description,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo,
        Status = rule.Status,
        CreatedBy = rule.CreatedBy,
        CreatedByName = rule.CreatedByNavigation?.FullName ?? rule.CreatedByNavigation?.Email,
        PublishedBy = rule.PublishedBy,
        PublishedByName = rule.PublishedByNavigation?.FullName ?? rule.PublishedByNavigation?.Email,
        PublishedAt = rule.PublishedAt,
        CancelledBy = rule.CancelledBy,
        CancelledByName = rule.CancelledByNavigation?.FullName ?? rule.CancelledByNavigation?.Email,
        CancelledAt = rule.CancelledAt,
        CancellationReason = rule.CancellationReason,
        CreatedAt = rule.CreatedAt,
        UpdatedAt = rule.UpdatedAt,
        PenaltyLevels = rule.AttendancePenaltyLevels
            .OrderBy(l => l.ViolationType)
            .ThenBy(l => l.FromMinutes)
            .Select(MapLevel)
            .ToList()
    };

    private static AttendancePenaltyLevelResponse MapLevel(AttendancePenaltyLevel level) => new()
    {
        PenaltyLevelId = level.PenaltyLevelId,
        PenaltyRuleId = level.PenaltyRuleId,
        ViolationType = level.ViolationType,
        FromMinutes = level.FromMinutes,
        ToMinutes = level.ToMinutes,
        PenaltyAmount = level.PenaltyAmount,
        CreatedAt = level.CreatedAt,
        UpdatedAt = level.UpdatedAt
    };
}

