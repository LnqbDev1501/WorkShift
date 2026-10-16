using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ScheduleWeekService(IScheduleWeekRepo scheduleWeekRepo) : IScheduleWeekService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "DRAFT",
        "OPEN_REGISTRATION",
        "REGISTRATION_CLOSED",
        "COMPLETED",
        "CANCELLED"
    };

    private static readonly HashSet<string> ActiveAssignmentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASSIGNED",
        "COMPLETED"
    };

    private static readonly TimeZoneInfo BusinessTimeZone = ResolveBusinessTimeZone();

    public async Task<List<ScheduleWeekSummaryResponse>> GetListAsync(
        Guid managerId,
        ScheduleWeekListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var manager = await GetManagerAsync(managerId);

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
            throw new ArgumentException("From date cannot be later than to date.");

        var status = NormalizeStatus(query.Status);
        var weeks = await scheduleWeekRepo.GetListAsync(
            manager.BranchId!.Value,
            query.FromDate,
            query.ToDate,
            status);

        return weeks.Select(MapSummary).ToList();
    }

    public async Task<ScheduleWeekDetailResponse> GetByIdAsync(Guid managerId, int scheduleWeekId)
    {
        ValidateId(scheduleWeekId);
        var manager = await GetManagerAsync(managerId);
        var week = await GetWeekAsync(scheduleWeekId, manager.BranchId!.Value);
        return MapDetail(week);
    }

    public async Task<ScheduleWeekDetailResponse> CreateAsync(
        Guid managerId,
        CreateScheduleWeekRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var manager = await GetManagerAsync(managerId);
        var deadline = ValidateWeekInput(request.WeekStartDate, request.RegistrationDeadline);

        if (await scheduleWeekRepo.ExistsAsync(manager.BranchId!.Value, request.WeekStartDate))
            throw new InvalidOperationException("A work schedule already exists for this branch and week.");

        var now = DateTime.UtcNow;
        var week = new WorkScheduleWeek
        {
            BranchId = manager.BranchId.Value,
            WeekStartDate = request.WeekStartDate,
            WeekEndDate = request.WeekStartDate.AddDays(6),
            RegistrationDeadline = deadline,
            Status = "DRAFT",
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await scheduleWeekRepo.AddAsync(week);
        return await GetCreatedOrUpdatedWeekAsync(week.ScheduleWeekId, manager.BranchId.Value);
    }

    public async Task<ScheduleWeekDetailResponse> UpdateAsync(
        Guid managerId,
        int scheduleWeekId,
        UpdateScheduleWeekRequest request)
    {
        ValidateId(scheduleWeekId);
        ArgumentNullException.ThrowIfNull(request);
        var manager = await GetManagerAsync(managerId);
        var branchId = manager.BranchId!.Value;
        var week = await GetWeekAsync(scheduleWeekId, branchId, tracking: true);

        EnsureStatus(week, "DRAFT", "Only a draft work schedule can be updated.");
        var deadline = ValidateWeekInput(request.WeekStartDate, request.RegistrationDeadline);

        if (week.WeekStartDate != request.WeekStartDate)
        {
            if (await scheduleWeekRepo.HasShiftsAsync(scheduleWeekId))
                throw new InvalidOperationException(
                    "The week start date cannot be changed after shifts have been created.");

            if (await scheduleWeekRepo.ExistsAsync(branchId, request.WeekStartDate, scheduleWeekId))
                throw new InvalidOperationException("A work schedule already exists for this branch and week.");
        }

        week.WeekStartDate = request.WeekStartDate;
        week.WeekEndDate = request.WeekStartDate.AddDays(6);
        week.RegistrationDeadline = deadline;
        week.UpdatedAt = DateTime.UtcNow;

        await scheduleWeekRepo.SaveChangesAsync();
        return await GetCreatedOrUpdatedWeekAsync(scheduleWeekId, branchId);
    }

    public async Task DeleteAsync(Guid managerId, int scheduleWeekId)
    {
        ValidateId(scheduleWeekId);
        var manager = await GetManagerAsync(managerId);
        var week = await GetWeekAsync(scheduleWeekId, manager.BranchId!.Value, tracking: true);

        EnsureStatus(week, "DRAFT", "Only a draft work schedule can be deleted.");

        if (await scheduleWeekRepo.HasDeleteBlockingDependenciesAsync(scheduleWeekId))
            throw new InvalidOperationException(
                "The work schedule has assignments or other dependent records and cannot be deleted.");

        await scheduleWeekRepo.DeleteDraftAsync(week);
    }

    public async Task<ScheduleWeekDetailResponse> OpenRegistrationAsync(Guid managerId, int scheduleWeekId)
    {
        ValidateId(scheduleWeekId);
        var manager = await GetManagerAsync(managerId);
        var branchId = manager.BranchId!.Value;
        var week = await GetWeekAsync(scheduleWeekId, branchId);

        EnsureStatus(week, "DRAFT", "Only a draft work schedule can be opened for registration.");

        var now = DateTime.UtcNow;
        if (week.RegistrationDeadline <= now)
            throw new InvalidOperationException("The registration deadline has already passed.");

        if (week.WorkShifts.Count == 0)
            throw new InvalidOperationException("At least one shift is required before registration can be opened.");

        foreach (var shift in week.WorkShifts)
        {
            if (!shift.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Shift {shift.ShiftId} is not in DRAFT status.");

            if (shift.ScheduledStartAt <= now || shift.ScheduledEndAt <= shift.ScheduledStartAt)
                throw new InvalidOperationException($"Shift {shift.ShiftId} has an invalid or elapsed schedule.");

            var localStartDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(AsUtc(shift.ScheduledStartAt), BusinessTimeZone));

            if (localStartDate < week.WeekStartDate || localStartDate > week.WeekEndDate)
                throw new InvalidOperationException($"Shift {shift.ShiftId} is outside the work week.");

            if (shift.ShiftEmployeeRequirements.Count == 0)
                throw new InvalidOperationException(
                    $"Shift {shift.ShiftId} must have at least one employee requirement.");

            if (shift.ShiftEmployeeRequirements.Any(requirement =>
                    requirement.RequiredCount <= 0 ||
                    !requirement.EmployeeType.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Shift {shift.ShiftId} contains an invalid or inactive employee type requirement.");
            }
        }

        if (!await scheduleWeekRepo.OpenRegistrationAsync(scheduleWeekId, branchId, now, managerId))
            throw new InvalidOperationException(
                "The work schedule status changed while the request was being processed. Reload and try again.");

        return await GetCreatedOrUpdatedWeekAsync(scheduleWeekId, branchId);
    }

    public async Task<ScheduleWeekDetailResponse> CloseRegistrationAsync(Guid managerId, int scheduleWeekId)
    {
        ValidateId(scheduleWeekId);
        var manager = await GetManagerAsync(managerId);
        var branchId = manager.BranchId!.Value;
        var week = await GetWeekAsync(scheduleWeekId, branchId);

        EnsureStatus(
            week,
            "OPEN_REGISTRATION",
            "Only a work schedule with open registration can be closed.");

        if (!await scheduleWeekRepo.CloseRegistrationAsync(scheduleWeekId, branchId, DateTime.UtcNow))
            throw new InvalidOperationException(
                "The work schedule status changed while the request was being processed. Reload and try again.");

        return await GetCreatedOrUpdatedWeekAsync(scheduleWeekId, branchId);
    }

    private async Task<User> GetManagerAsync(Guid managerId)
    {
        if (managerId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated manager ID is invalid.");

        var manager = await scheduleWeekRepo.GetActiveManagerAsync(managerId);
        if (manager == null)
            throw new UnauthorizedAccessException("The authenticated account is not an active manager.");

        if (!manager.BranchId.HasValue)
            throw new InvalidOperationException("The manager is not assigned to a branch.");

        if (manager.Branch == null || !manager.Branch.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The manager's branch is not active.");

        return manager;
    }

    private async Task<WorkScheduleWeek> GetWeekAsync(
        int scheduleWeekId,
        int branchId,
        bool tracking = false) =>
        await scheduleWeekRepo.GetDetailAsync(scheduleWeekId, branchId, tracking)
        ?? throw new KeyNotFoundException("Work schedule not found.");

    private async Task<ScheduleWeekDetailResponse> GetCreatedOrUpdatedWeekAsync(int scheduleWeekId, int branchId)
    {
        var week = await scheduleWeekRepo.GetDetailAsync(scheduleWeekId, branchId)
            ?? throw new InvalidOperationException("The saved work schedule could not be loaded.");
        return MapDetail(week);
    }

    private static DateTime ValidateWeekInput(DateOnly weekStartDate, DateTimeOffset registrationDeadline)
    {
        if (weekStartDate == default)
            throw new ArgumentException("Week start date is required.");

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("Week start date must be a Monday.");

        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BusinessTimeZone).DateTime);
        if (weekStartDate <= localToday)
            throw new ArgumentException("Week start date must be in the future.");

        if (registrationDeadline == default)
            throw new ArgumentException("Registration deadline is required.");

        if (registrationDeadline <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Registration deadline must be in the future.");

        var localDeadline = TimeZoneInfo.ConvertTime(registrationDeadline, BusinessTimeZone);
        if (DateOnly.FromDateTime(localDeadline.DateTime) >= weekStartDate)
            throw new ArgumentException("Registration deadline must be before the work week starts.");

        return registrationDeadline.UtcDateTime;
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var normalized = status.Trim().ToUpperInvariant();
        if (!AllowedStatuses.Contains(normalized))
            throw new ArgumentException(
                "Status must be DRAFT, OPEN_REGISTRATION, REGISTRATION_CLOSED, COMPLETED, or CANCELLED.");
        return normalized;
    }

    private static void ValidateId(int scheduleWeekId)
    {
        if (scheduleWeekId <= 0)
            throw new ArgumentException("Schedule week ID must be greater than zero.");
    }

    private static void EnsureStatus(WorkScheduleWeek week, string expected, string message)
    {
        if (!week.Status.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(message);
    }

    private static ScheduleWeekSummaryResponse MapSummary(WorkScheduleWeek week)
    {
        var shifts = week.WorkShifts.Where(shift => shift.Status != "CANCELLED").ToList();
        return new ScheduleWeekSummaryResponse
        {
            ScheduleWeekId = week.ScheduleWeekId,
            BranchId = week.BranchId,
            WeekStartDate = week.WeekStartDate,
            WeekEndDate = week.WeekEndDate,
            RegistrationDeadline = week.RegistrationDeadline,
            Status = week.Status,
            OpenedAt = week.OpenedAt,
            RegistrationClosedAt = week.RegistrationClosedAt,
            TotalShifts = shifts.Count,
            RequiredEmployees = shifts.Sum(shift => shift.ShiftEmployeeRequirements.Sum(r => r.RequiredCount)),
            AssignedEmployees = shifts.Sum(shift => shift.ShiftAssignments.Count(IsActiveAssignment)),
            CreatedAt = week.CreatedAt,
            UpdatedAt = week.UpdatedAt
        };
    }

    private static ScheduleWeekDetailResponse MapDetail(WorkScheduleWeek week)
    {
        var summary = MapSummary(week);
        return new ScheduleWeekDetailResponse
        {
            ScheduleWeekId = summary.ScheduleWeekId,
            BranchId = summary.BranchId,
            WeekStartDate = summary.WeekStartDate,
            WeekEndDate = summary.WeekEndDate,
            RegistrationDeadline = summary.RegistrationDeadline,
            Status = summary.Status,
            OpenedAt = summary.OpenedAt,
            RegistrationClosedAt = summary.RegistrationClosedAt,
            TotalShifts = summary.TotalShifts,
            RequiredEmployees = summary.RequiredEmployees,
            AssignedEmployees = summary.AssignedEmployees,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            Shifts = week.WorkShifts
                .OrderBy(shift => shift.ScheduledStartAt)
                .Select(MapShift)
                .ToList()
        };
    }

    private static ScheduleWeekShiftResponse MapShift(WorkShift shift)
    {
        var requirements = shift.ShiftEmployeeRequirements
            .OrderBy(requirement => requirement.EmployeeType.TypeName)
            .Select(requirement => new ShiftEmployeeRequirementResponse
            {
                ShiftRequirementId = requirement.ShiftRequirementId,
                EmployeeTypeId = requirement.EmployeeTypeId,
                EmployeeTypeName = requirement.EmployeeType.TypeName,
                RequiredCount = requirement.RequiredCount,
                AssignedCount = shift.ShiftAssignments.Count(assignment =>
                    assignment.ShiftRequirementId == requirement.ShiftRequirementId &&
                    IsActiveAssignment(assignment))
            })
            .ToList();

        return new ScheduleWeekShiftResponse
        {
            ShiftId = shift.ShiftId,
            ShiftTemplateId = shift.ShiftTemplateId,
            ShiftName = shift.ShiftNameSnapshot,
            ScheduledStartAt = shift.ScheduledStartAt,
            ScheduledEndAt = shift.ScheduledEndAt,
            BreakMinutes = 0,
            Note = shift.Note,
            Status = shift.Status,
            RequiredEmployees = requirements.Sum(requirement => requirement.RequiredCount),
            AssignedEmployees = shift.ShiftAssignments.Count(IsActiveAssignment),
            EmployeeRequirements = requirements
        };
    }

    private static bool IsActiveAssignment(ShiftAssignment assignment) =>
        ActiveAssignmentStatuses.Contains(assignment.Status);

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "UTC+07",
            TimeSpan.FromHours(7),
            "UTC+07",
            "UTC+07");
    }
}
