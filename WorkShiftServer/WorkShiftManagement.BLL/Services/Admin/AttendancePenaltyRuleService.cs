using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
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
    private readonly ILogger<AttendancePenaltyRuleService> _logger;

    public AttendancePenaltyRuleService(
        IAttendancePenaltyRuleRepo attendancePenaltyRuleRepo,
        ILogger<AttendancePenaltyRuleService> logger)
    {
        _attendancePenaltyRuleRepo = attendancePenaltyRuleRepo;
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
                ToMinutes = level.ToMinutes!.Value,
                PenaltyAmount = normalizedAmount,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        var created = await _attendancePenaltyRuleRepo.CreateAsync(rule, cancellationToken);
        return await GetByIdAsync(created.PenaltyRuleId, cancellationToken);
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

            if (!level.FromMinutes.HasValue || level.FromMinutes.Value < 1 || level.FromMinutes.Value > 1440)
            {
                throw new ArgumentException("fromMinutes must be a valid integer between 1 and 1440.");
            }

            if (!level.ToMinutes.HasValue || level.ToMinutes.Value < level.FromMinutes.Value || level.ToMinutes.Value > 1440)
            {
                throw new ArgumentException("toMinutes must be a valid integer between fromMinutes and 1440.");
            }

            if (!level.PenaltyAmount.HasValue || level.PenaltyAmount.Value <= 0)
            {
                throw new ArgumentException("penaltyAmount must be greater than 0.");
            }
        }

        // Validate overlapping intervals per violation type
        var grouped = request.PenaltyLevels
            .GroupBy(l => NormalizeViolationType(l.ViolationType));

        foreach (var group in grouped)
        {
            var sortedLevels = group.OrderBy(l => l.FromMinutes!.Value).ToList();
            for (var i = 0; i < sortedLevels.Count - 1; i++)
            {
                var current = sortedLevels[i];
                var next = sortedLevels[i + 1];

                if (current.ToMinutes!.Value >= next.FromMinutes!.Value)
                {
                    throw new ArgumentException(
                        $"Penalty levels for {group.Key} have overlapping minute ranges ({current.FromMinutes}-{current.ToMinutes} and {next.FromMinutes}-{next.ToMinutes}).");
                }
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

