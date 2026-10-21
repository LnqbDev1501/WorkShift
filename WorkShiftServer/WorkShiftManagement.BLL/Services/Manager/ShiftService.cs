using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ShiftService(IShiftRepo repo, IScheduleWeekRepo weekRepo) : IShiftService
{
    public async Task<ShiftOptionsResponse> GetOptionsAsync(Guid managerId, CancellationToken ct = default)
    {
        var branchId = await GetBranchAsync(managerId);
        // Sequential queries share a scoped DbContext.
        var templates = await repo.GetActiveTemplatesAsync(ct);
        var types = await repo.GetActiveTypesAsync(ct);
        var staff = await repo.GetActiveStaffAsync(branchId, ct);
        return new ShiftOptionsResponse
        {
            Templates = templates.Select(x => new ShiftTemplateOptionResponse { Id = x.ShiftTemplateId, Name = x.ShiftName,
                StartTime = x.StartTime, EndTime = x.EndTime }).ToList(),
            EmployeeTypes = types.Select(x => new ShiftTypeOptionResponse { Id = x.EmployeeTypeId, Name = x.TypeName }).ToList(),
            Staff = staff.Select(x => new ShiftStaffOptionResponse { UserId = x.UserId, FullName = x.FullName,
                EmployeeTypeId = x.EmployeeTypeId!.Value, EmployeeTypeName = x.EmployeeType!.TypeName }).ToList()
        };
    }
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<ShiftDetailResponse> CreateAsync(Guid managerId, int weekId, CreateShiftRequest request, CancellationToken ct = default)
    {
        ValidateId(weekId);
        ValidateRequest(request);
        var branchId = await GetBranchAsync(managerId);
        WorkShift shift = new();
        await repo.ExecuteDraftAsync(weekId, branchId, async week =>
        {
            var data = await ValidateScheduleAsync(week, request, null, ct);
            var now = DateTime.UtcNow;
            shift = new WorkShift
            {
                ScheduleWeekId = weekId, CreatedBy = managerId,
                CreatedAt = now, UpdatedAt = now, Status = "DRAFT"
            };
            Apply(shift, request, data.Template, data.Start, data.End, now);
            SynchronizeRequirements(shift, request.EmployeeRequirements, now);
            repo.Add(shift);
        }, ct);
        return await GetDetailAsync(shift.ShiftId, branchId, ct);
    }

    public async Task<ShiftDetailResponse> GetByIdAsync(Guid managerId, int shiftId, CancellationToken ct = default)
    {
        ValidateId(shiftId);
        return await GetDetailAsync(shiftId, await GetBranchAsync(managerId), ct);
    }

    public async Task<ShiftDetailResponse> UpdateAsync(Guid managerId, int shiftId, UpdateShiftRequest request, CancellationToken ct = default)
    {
        ValidateId(shiftId);
        ValidateRequest(request);
        var branchId = await GetBranchAsync(managerId);
        var existing = await repo.GetByIdAsync(shiftId, branchId, ct)
            ?? throw new KeyNotFoundException("Shift not found.");
        await repo.ExecuteDraftAsync(existing.ScheduleWeekId, branchId, async week =>
        {
            var shift = await GetEditableAsync(shiftId, branchId, ct);
            var data = await ValidateScheduleAsync(week, request, shiftId, ct);
            var now = DateTime.UtcNow;
            Apply(shift, request, data.Template, data.Start, data.End, now);
            SynchronizeRequirements(shift, request.EmployeeRequirements, now);
        }, ct);
        return await GetDetailAsync(shiftId, branchId, ct);
    }

    public async Task DeleteAsync(Guid managerId, int shiftId, CancellationToken ct = default)
    {
        ValidateId(shiftId);
        var branchId = await GetBranchAsync(managerId);
        var existing = await repo.GetByIdAsync(shiftId, branchId, ct)
            ?? throw new KeyNotFoundException("Shift not found.");
        await repo.ExecuteDraftAsync(existing.ScheduleWeekId, branchId, async _ =>
        {
            var shift = await GetEditableAsync(shiftId, branchId, ct);
            repo.RemoveRequirements(shift.ShiftEmployeeRequirements.ToList());
            repo.Remove(shift);
        }, ct);
    }

    private async Task<WorkShift> GetEditableAsync(int shiftId, int branchId, CancellationToken ct)
    {
        var shift = await repo.GetForUpdateAsync(shiftId, branchId, ct)
            ?? throw new KeyNotFoundException("Shift not found.");
        if (shift.Status != "DRAFT")
            throw new InvalidOperationException("Only draft shifts can be changed.");
        if (await repo.HasDependenciesAsync(shiftId, ct))
            throw new InvalidOperationException("The shift has assignments, checklists or adjustment requests and cannot be changed.");
        return shift;
    }

    private async Task<int> GetBranchAsync(Guid managerId)
    {
        if (managerId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated manager ID is invalid.");
        var manager = await weekRepo.GetActiveManagerAsync(managerId)
            ?? throw new UnauthorizedAccessException("The account is not an active manager.");
        if (manager.BranchId is not int branchId || manager.Branch?.Status != "ACTIVE" || manager.Role.Status != "ACTIVE")
            throw new UnauthorizedAccessException("An active manager role and active assigned branch are required.");
        return branchId;
    }

    private static void ValidateId(int id)
    {
        if (id <= 0) throw new ArgumentException("ID must be greater than zero.");
    }

    private static void ValidateRequest(CreateShiftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.ShiftTemplateId);
        if (request.WorkDate == default || request.WorkDate == DateOnly.MaxValue)
            throw new ArgumentException("A valid work date is required.");
        if (request.Note?.Length > 500)
            throw new ArgumentException("Note cannot exceed 500 characters.");
        if (request.EmployeeRequirements is not { Count: > 0 })
            throw new ArgumentException("At least one employee requirement is required.");
        if (request.EmployeeRequirements.Any(x => x == null || x.EmployeeTypeId <= 0 || x.RequiredCount <= 0))
            throw new ArgumentException("Employee type IDs and required counts must be greater than zero.");
        if (request.EmployeeRequirements.Select(x => x.EmployeeTypeId).Distinct().Count() != request.EmployeeRequirements.Count)
            throw new ArgumentException("An employee type cannot appear more than once.");
        if (request.EmployeeRequirements.Sum(x => (long)x.RequiredCount) > int.MaxValue)
            throw new ArgumentException("The total required employee count is too large.");
    }

    private async Task<(ShiftTemplate Template, DateTime Start, DateTime End)> ValidateScheduleAsync(
        WorkScheduleWeek week, CreateShiftRequest request, int? excludeId, CancellationToken ct)
    {
        if (request.WorkDate < week.WeekStartDate || request.WorkDate > week.WeekEndDate)
            throw new ArgumentException("Work date must be inside the selected week.");
        var template = await repo.GetTemplateAsync(request.ShiftTemplateId, ct);
        if (template == null || template.Status != "ACTIVE")
            throw new ArgumentException("The selected shift template does not exist or is inactive.");
        if (template.StartTime == template.EndTime)
            throw new ArgumentException("The shift template must have different start and end times.");
        var start = new DateTimeOffset(request.WorkDate.ToDateTime(template.StartTime), VietnamOffset).UtcDateTime;
        var endDate = template.EndTime < template.StartTime ? request.WorkDate.AddDays(1) : request.WorkDate;
        var end = new DateTimeOffset(endDate.ToDateTime(template.EndTime), VietnamOffset).UtcDateTime;
        if (start <= DateTime.UtcNow)
            throw new ArgumentException("The shift must start in the future.");
        if (week.RegistrationDeadline >= start)
            throw new ArgumentException("The shift must start after the registration deadline.");
        var types = await repo.GetEmployeeTypesAsync(request.EmployeeRequirements.Select(x => x.EmployeeTypeId), ct);
        if (types.Count != request.EmployeeRequirements.Count || types.Any(x => x.Status != "ACTIVE"))
            throw new ArgumentException("All selected employee types must exist and be active.");
        if (await repo.ExistsAsync(week.ScheduleWeekId, start, end, excludeId, ct))
            throw new InvalidOperationException("A shift with the same time range already exists in this week.");
        return (template, start, end);
    }

    private static void Apply(WorkShift shift, CreateShiftRequest request, ShiftTemplate template,
        DateTime start, DateTime end, DateTime now)
    {
        shift.ShiftTemplateId = template.ShiftTemplateId;
        shift.ShiftNameSnapshot = template.ShiftName;
        shift.ScheduledStartAt = start;
        shift.ScheduledEndAt = end;
        shift.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        // Compatibility with the existing NOT NULL column; responses derive totals from requirements.
        shift.RequiredEmployees = request.EmployeeRequirements.Sum(x => x.RequiredCount);
        shift.UpdatedAt = now;
    }

    private void SynchronizeRequirements(WorkShift shift, List<ShiftEmployeeRequirementRequest> requests, DateTime now)
    {
        var ids = requests.Select(x => x.EmployeeTypeId).ToHashSet();
        var removed = shift.ShiftEmployeeRequirements.Where(x => !ids.Contains(x.EmployeeTypeId)).ToList();
        repo.RemoveRequirements(removed);
        foreach (var item in removed) shift.ShiftEmployeeRequirements.Remove(item);
        foreach (var request in requests)
        {
            var requirement = shift.ShiftEmployeeRequirements.SingleOrDefault(x => x.EmployeeTypeId == request.EmployeeTypeId);
            if (requirement == null)
            {
                requirement = new ShiftEmployeeRequirement
                {
                    EmployeeTypeId = request.EmployeeTypeId, CreatedAt = now
                };
                shift.ShiftEmployeeRequirements.Add(requirement);
            }
            requirement.RequiredCount = request.RequiredCount;
            requirement.UpdatedAt = now;
        }
    }

    private async Task<ShiftDetailResponse> GetDetailAsync(int shiftId, int branchId, CancellationToken ct)
    {
        var shift = await repo.GetByIdAsync(shiftId, branchId, ct)
            ?? throw new KeyNotFoundException("Shift not found.");
        return new ShiftDetailResponse
        {
            ShiftId = shift.ShiftId, ScheduleWeekId = shift.ScheduleWeekId,
            ShiftTemplateId = shift.ShiftTemplateId, ShiftName = shift.ShiftNameSnapshot,
            ScheduledStartAt = shift.ScheduledStartAt, ScheduledEndAt = shift.ScheduledEndAt,
            Note = shift.Note, Status = shift.Status,
            CreatedAt = shift.CreatedAt, UpdatedAt = shift.UpdatedAt,
            EmployeeRequirements = shift.ShiftEmployeeRequirements.OrderBy(x => x.EmployeeTypeId).Select(r =>
                new ShiftEmployeeRequirementResponse
                {
                    ShiftRequirementId = r.ShiftRequirementId, EmployeeTypeId = r.EmployeeTypeId,
                    EmployeeTypeName = r.EmployeeType.TypeName, RequiredCount = r.RequiredCount,
                    AssignedCount = shift.ShiftAssignments.Count(a => a.ShiftRequirementId == r.ShiftRequirementId &&
                        a.Status is "ASSIGNED" or "COMPLETED")
                }).ToList(),
            Staff = shift.ShiftAssignments.OrderBy(x => x.AssignedAt).ThenBy(x => x.AssignmentId).Select(a =>
                new ShiftStaffResponse
                {
                    AssignmentId = a.AssignmentId, ShiftRequirementId = a.ShiftRequirementId,
                    UserId = a.UserId, FullName = a.User.FullName, AssignmentSource = a.AssignmentSource,
                    AssignedAt = a.AssignedAt, Status = a.Status
                }).ToList()
        };
    }
}
