using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;
using static WorkShiftManagement.BLL.Helpers.ShiftAssignmentRules;
using static WorkShiftManagement.BLL.Helpers.StaffAccountRules;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public sealed class StaffShiftService(IStaffShiftRepo repo, IShiftAssignmentRepo assignments) : IStaffShiftService
{
    public Task<PagedResponse<StaffShiftResponse>> GetAvailableAsync(Guid staffId, StaffShiftListQuery query,
        CancellationToken ct = default) => GetPagedAsync(staffId, query, true, ct);

    public Task<PagedResponse<StaffShiftResponse>> GetMyScheduleAsync(Guid staffId, StaffShiftListQuery query,
        CancellationToken ct = default) => GetPagedAsync(staffId, query, false, ct);

    private async Task<PagedResponse<StaffShiftResponse>> GetPagedAsync(Guid staffId, StaffShiftListQuery query,
        bool availableOnly, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100 ||
            (long)(query.PageNumber - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentException("Page number must be positive and page size must be between 1 and 100.");
        if (query.FromDate > query.ToDate || query.ToDate == DateOnly.MaxValue || query.FromDate == DateOnly.MinValue)
            throw new ArgumentException("Invalid date range.");
        var staff = await GetStaffAsync(staffId, ct);
        if (availableOnly) ValidateEmployeeType(staff);
        var (items, total) = await repo.GetPagedAsync(staffId, staff.BranchId!.Value,
            staff.EmployeeTypeId, availableOnly,
            query.FromDate.HasValue ? ToUtc(query.FromDate.Value) : null,
            query.ToDate.HasValue ? ToUtc(query.ToDate.Value.AddDays(1)) : null,
            query.PageNumber, query.PageSize, DateTime.UtcNow, ct);
        return new PagedResponse<StaffShiftResponse>
        {
            Items = items.Select(s => Map(s, staffId, staff.BranchId!.Value)).ToList(), TotalItems = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        };
    }

    public async Task<StaffShiftResponse> GetDetailAsync(Guid staffId, int shiftId, CancellationToken ct = default)
    {
        ValidateShiftId(shiftId);
        var staff = await GetStaffAsync(staffId, ct);
        var shift = await repo.GetDetailAsync(shiftId, staffId, staff.BranchId!.Value, ct)
            ?? throw new KeyNotFoundException("Shift not found.");
        return Map(shift, staffId, staff.BranchId!.Value);
    }

    public async Task<StaffShiftRegistrationResponse> RegisterAsync(Guid staffId, int shiftId, CancellationToken ct = default)
    {
        ValidateShiftId(shiftId);
        var staff = await GetStaffAsync(staffId, ct);
        var branchId = staff.BranchId!.Value;
        var result = await assignments.ExecuteAsync(shiftId, staffId, branchId, async (shift, lockedStaff) =>
        {
            // Recheck user state under the same locks used by manager assignments.
            ValidateStaff(lockedStaff, branchId);
            ValidateEmployeeType(lockedStaff);
            ValidateRegistrationWindow(shift);
            // This query participates in the assignment's serializable transaction, so a stale list cannot authorize registration.
            if (!await repo.HasRegistrationContractAsync(staffId, branchId, shiftId, DateTime.UtcNow, ct))
                throw new InvalidOperationException("You must have an accepted, ACTIVE employment contract in this branch that is effective now and covers the entire shift. Shifts before the contract start date, after its end date, or on/after its termination date cannot be registered.");
            var requirement = shift.ShiftEmployeeRequirements.SingleOrDefault(r => r.EmployeeTypeId == lockedStaff.EmployeeTypeId)
                ?? throw new InvalidOperationException("The shift does not require your employee type.");
            if (requirement.RequiredCount <= 0 || requirement.EmployeeType.Status != "ACTIVE")
                throw new InvalidOperationException("The employee requirement is not active.");
            var existing = shift.ShiftAssignments.SingleOrDefault(a => a.UserId == staffId);
            if (existing != null && existing.Status != "CANCELLED")
                throw new InvalidOperationException("You are already assigned to this shift.");
            if (existing != null) ValidateHistory(existing, restoring: true);
            ValidateCapacity(shift, requirement);

            var firstDay = DateOnly.FromDateTime(shift.ScheduledStartAt.AddHours(7));
            var lastDay = DateOnly.FromDateTime(shift.ScheduledEndAt.AddTicks(-1).AddHours(7));
            var others = await assignments.GetStaffShiftsAsync(staffId,
                ToUtc(StartOfWeek(firstDay)),
                ToUtc(StartOfWeek(lastDay).AddDays(7)), ct);
            if (others.Any(s => s.ScheduledStartAt < shift.ScheduledEndAt && s.ScheduledEndAt > shift.ScheduledStartAt))
                throw new InvalidOperationException("You have an overlapping shift.");
            if (await assignments.HasApprovedLeaveAsync(staffId, shift.ScheduledStartAt, shift.ScheduledEndAt, ct))
                throw new InvalidOperationException("You have approved leave during this shift.");
            ValidateWorkingTime(shift, others, await assignments.GetRulesAsync(ct));

            var now = DateTime.UtcNow;
            var assignment = existing ?? new ShiftAssignment { ShiftId = shiftId, UserId = staffId, CreatedAt = now };
            var oldValue = existing == null ? null : JsonSerializer.Serialize(MapRegistration(existing));
            assignment.ShiftRequirementId = requirement.ShiftRequirementId;
            assignment.AssignmentSource = "SELF_REGISTRATION";
            assignment.AssignedBy = staffId;
            assignment.AssignedAt = now;
            assignment.Status = "ASSIGNED";
            assignment.CancelledBy = null;
            assignment.CancelledAt = null;
            assignment.CancelReason = null;
            assignment.UpdatedAt = now;
            if (existing == null) assignments.Add(assignment);
            else assignments.Audit(staffId, "RESTORE_SHIFT_REGISTRATION", assignment, oldValue);
            return assignment;
        }, ct);
        return MapRegistration(result);
    }

    public async Task<StaffShiftRegistrationResponse> CancelAsync(Guid staffId, int shiftId,
        CancelShiftRegistrationRequest request, CancellationToken ct = default)
    {
        ValidateShiftId(shiftId);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ArgumentException("Cancellation reason must contain 1 to 500 characters.");
        var staff = await GetStaffAsync(staffId, ct);
        var branchId = staff.BranchId!.Value;
        var result = await assignments.ExecuteAsync(shiftId, staffId, branchId, async (shift, lockedStaff) =>
        {
            ValidateStaff(lockedStaff, branchId);
            var assignment = shift.ShiftAssignments.SingleOrDefault(a => a.UserId == staffId)
                ?? throw new KeyNotFoundException("Your shift registration was not found.");
            var blockedReason = GetCancellationBlockedReason(shift, assignment, branchId);
            if (blockedReason != null) throw new InvalidOperationException(blockedReason);
            var oldValue = JsonSerializer.Serialize(MapRegistration(assignment));
            var now = DateTime.UtcNow;
            var reason = request.Reason.Trim();
            await assignments.CancelPendingSwapsAsync(assignment.AssignmentId, staffId, now, reason, ct);
            assignment.Status = "CANCELLED";
            assignment.CancelledBy = staffId;
            assignment.CancelledAt = now;
            assignment.CancelReason = reason;
            assignment.UpdatedAt = now;
            assignments.Audit(staffId, "CANCEL_SHIFT_REGISTRATION", assignment, oldValue);
            return assignment;
        }, ct);
        return MapRegistration(result);
    }

    private async Task<User> GetStaffAsync(Guid staffId, CancellationToken ct)
    {
        if (staffId == Guid.Empty) throw new UnauthorizedAccessException("Invalid authenticated user ID.");
        var staff = await repo.GetStaffAsync(staffId, ct)
            ?? throw new UnauthorizedAccessException("An active staff account is required.");
        ValidateStaff(staff, staff.BranchId);
        return staff;
    }

    private static void ValidateEmployeeType(User staff)
    {
        if (!staff.EmployeeTypeId.HasValue || staff.EmployeeType?.Status != "ACTIVE")
            throw new InvalidOperationException("An active employee type is required to register for shifts.");
    }

    private static void ValidateRegistrationWindow(WorkShift shift)
    {
        var now = DateTime.UtcNow;
        if (shift.Status != "SCHEDULED" || shift.ScheduledStartAt <= now || shift.ScheduledEndAt <= shift.ScheduledStartAt)
            throw new InvalidOperationException("Registration is closed, the shift has already started, or the shift is no longer available.");

        if (shift.ScheduleWeek.Status != "OPEN_REGISTRATION" || shift.ScheduleWeek.RegistrationDeadline <= now)
            throw new InvalidOperationException("Shift registration and cancellation are only allowed during OPEN_REGISTRATION and before the registration deadline.");
    }

    private static void ValidateShiftId(int shiftId)
    {
        if (shiftId <= 0) throw new ArgumentException("Shift ID must be positive.");
    }

    private static StaffShiftRegistrationResponse MapRegistration(ShiftAssignment assignment) => new()
    {
        AssignmentId = assignment.AssignmentId, ShiftId = assignment.ShiftId,
        ShiftRequirementId = assignment.ShiftRequirementId, AssignmentSource = assignment.AssignmentSource,
        Status = assignment.Status, AssignedAt = assignment.AssignedAt,
        CancelledAt = assignment.CancelledAt, CancelReason = assignment.CancelReason
    };

    private static string? GetCancellationBlockedReason(WorkShift shift, ShiftAssignment? assignment, int branchId)
    {
        if (assignment == null) return "You have not registered for this shift.";
        if (assignment.AssignmentSource != "SELF_REGISTRATION")
            return "Only shifts you registered for yourself can be cancelled. Contact your manager for other assignments.";
        if (assignment.Status != "ASSIGNED") return "Only active registrations can be cancelled.";
        if (shift.ScheduleWeek.BranchId != branchId) return "This registration belongs to your previous branch. Contact your manager.";
        try
        {
            ValidateRegistrationWindow(shift);
            ValidateHistory(assignment, restoring: false);
            return null;
        }
        catch (InvalidOperationException ex) { return ex.Message; }
    }

    private static StaffShiftResponse Map(WorkShift shift, Guid staffId, int branchId)
    {
        var own = shift.ShiftAssignments.FirstOrDefault(a => a.UserId == staffId);
        var blockedReason = GetCancellationBlockedReason(shift, own, branchId);
        return new()
        {
            ShiftId = shift.ShiftId, ScheduleWeekId = shift.ScheduleWeekId,
            BranchId = shift.ScheduleWeek.BranchId, BranchName = shift.ScheduleWeek.Branch.BranchName,
            ShiftName = shift.ShiftNameSnapshot, ScheduledStartAt = shift.ScheduledStartAt,
            ScheduledEndAt = shift.ScheduledEndAt, Status = shift.Status,
            ScheduleWeekStatus = shift.ScheduleWeek.Status, RegistrationDeadline = shift.ScheduleWeek.RegistrationDeadline,
            Note = shift.Note,
            MyRegistration = own == null ? null : MapRegistration(own),
            CanCancelRegistration = blockedReason == null,
            CancellationBlockedReason = blockedReason,
            IsOpenShift = false,
            EmployeeRequirements = shift.ShiftEmployeeRequirements.OrderBy(r => r.EmployeeTypeId).Select(r =>
            {
                var count = shift.ShiftAssignments.Count(a => a.ShiftRequirementId == r.ShiftRequirementId &&
                    a.Status is "ASSIGNED" or "COMPLETED");
                return new StaffShiftRequirementResponse
                {
                    ShiftRequirementId = r.ShiftRequirementId, EmployeeTypeId = r.EmployeeTypeId,
                    EmployeeTypeName = r.EmployeeType.TypeName, RequiredCount = r.RequiredCount,
                    AssignedCount = count, RemainingCount = shift.Status == "CANCELLED" ? 0 : Math.Max(0, r.RequiredCount - count)
                };
            }).ToList()
        };
    }
}
