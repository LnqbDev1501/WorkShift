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

    private static AttendancePenaltyRuleItemResponse MapListItem(AttendancePenaltyRule rule) => new()
    {
        PenaltyRuleId = rule.PenaltyRuleId,
        RuleName = rule.RuleName,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo,
        Status = rule.Status,
        CreatedAt = rule.CreatedAt
    };
}
