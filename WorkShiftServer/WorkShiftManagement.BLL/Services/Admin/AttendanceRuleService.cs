using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class AttendanceRuleService : IAttendanceRuleService
{
    private const string StatusDraft = "DRAFT";
    private readonly IAttendanceRuleRepo _attendanceRuleRepo;

    public AttendanceRuleService(IAttendanceRuleRepo attendanceRuleRepo)
    {
        _attendanceRuleRepo = attendanceRuleRepo;
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
