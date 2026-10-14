using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Interfaces.Staff;
using static WorkShiftManagement.BLL.Helpers.ShiftAssignmentRules;
using static WorkShiftManagement.BLL.Helpers.StaffAccountRules;

namespace WorkShiftManagement.BLL.Services.Staff;

public sealed class StaffShiftSwapService(IStaffShiftSwapRepo repo, IStaffShiftRepo staffRepo,
    IShiftAssignmentRepo assignments) : IStaffShiftSwapService
{
    public async Task<List<StaffShiftSwapCandidateResponse>> GetCandidatesAsync(Guid actor, int sourceAssignmentId, CancellationToken ct = default)
    {
        ValidateId(sourceAssignmentId);
        var staff = await GetActorAsync(actor, ct);
        var source = await repo.FindAssignmentAsync(sourceAssignmentId, ct)
            ?? throw new KeyNotFoundException("Your shift assignment was not found.");
        if (source.UserId != actor) throw new UnauthorizedAccessException("You can only offer your own assignment.");
        if (!staff.EmployeeTypeId.HasValue || source.Status != "ASSIGNED" || source.Shift.ScheduledStartAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Choose a future assigned shift.");
        ValidateStaff(source.User, staff.BranchId);
        ValidateSwappableAssignment(source, staff.BranchId!.Value);
        if (await repo.HasPendingAsync(sourceAssignmentId, sourceAssignmentId, ct))
            throw new InvalidOperationException("Your shift already has a pending swap request.");
        var candidates = await repo.GetCandidateAssignmentsAsync(actor, staff.BranchId!.Value, staff.EmployeeTypeId.Value, ct);
        var result = new List<StaffShiftSwapCandidateResponse>();
        foreach (var target in candidates)
        {
            try
            {
                ValidatePair(source, target, staff.BranchId.Value);
                await ValidateIncomingShiftAsync(source.UserId, source.Shift, target.Shift, ct);
                await ValidateIncomingShiftAsync(target.UserId, target.Shift, source.Shift, ct);
            }
            catch (InvalidOperationException) { continue; }
            catch (UnauthorizedAccessException) { continue; }
            result.Add(new() { StaffId = target.UserId, StaffName = target.User.FullName, Assignment = MapAssignment(target) });
        }
        return result;
    }

    public async Task<PagedResponse<StaffShiftSwapResponse>> GetPagedAsync(Guid actor, ShiftSwapListQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100 || (long)(query.PageNumber - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentException("Page number must be positive and page size must be between 1 and 100.");
        var status = Normalize(query.Status);
        if (status != null && status is not ("PENDING" or "ACCEPTED" or "REJECTED" or "CANCELLED"))
            throw new ArgumentException("Status must be PENDING, ACCEPTED, REJECTED, or CANCELLED.");
        var direction = Normalize(query.Direction) ?? "ALL";
        if (direction is not ("ALL" or "SENT" or "RECEIVED"))
            throw new ArgumentException("Direction must be ALL, SENT, or RECEIVED.");
        await GetActorAsync(actor, ct);
        var (items, total) = await repo.GetPagedAsync(actor, status, direction, query.PageNumber, query.PageSize, ct);
        return new()
        {
            Items = items.Select(Map).ToList(), TotalItems = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        };
    }

    public async Task<StaffShiftSwapResponse> GetDetailAsync(Guid actor, int requestId, CancellationToken ct = default)
    {
        ValidateId(requestId);
        await GetActorAsync(actor, ct);
        return Map(await FindAsync(actor, requestId, ct));
    }

    public async Task<StaffShiftSwapResponse> SubmitAsync(Guid actor, SubmitShiftSwapRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.SourceAssignmentId);
        ValidateId(request.TargetAssignmentId);
        if (request.SourceAssignmentId == request.TargetAssignmentId)
            throw new ArgumentException("Source and target assignments must be different.");
        var reason = CleanNote(request.Reason);
        var staff = await GetActorAsync(actor, ct);
        var branchId = staff.BranchId!.Value;
        var result = await repo.ExecuteAsync(request.SourceAssignmentId, request.TargetAssignmentId, null,
            async (source, target, _) =>
            {
                if (source.UserId != actor) throw new UnauthorizedAccessException("You can only offer your own assignment.");
                ValidatePair(source, target, branchId);
                if (await repo.HasPendingAsync(source.AssignmentId, target.AssignmentId, ct))
                    throw new InvalidOperationException("An assignment already has a pending swap request.");
                await ValidateIncomingShiftAsync(source.UserId, source.Shift, target.Shift, ct);
                await ValidateIncomingShiftAsync(target.UserId, target.Shift, source.Shift, ct);
                var now = DateTime.UtcNow;
                var swap = new ShiftSwapRequest
                {
                    RequesterId = actor, TargetUserId = target.UserId,
                    SourceAssignmentId = source.AssignmentId, TargetAssignmentId = target.AssignmentId,
                    SourceAssignment = source, TargetAssignment = target,
                    Requester = source.User, TargetUser = target.User,
                    Reason = reason, Status = "PENDING", CreatedAt = now, UpdatedAt = now
                };
                repo.Add(swap);
                return swap;
            }, ct);
        return Map(result);
    }

    public Task<StaffShiftSwapResponse> CancelAsync(Guid actor, int requestId, RespondShiftSwapRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RespondAsync(actor, requestId, "CANCELLED", CleanNote(request.Note), ct);
    }

    public Task<StaffShiftSwapResponse> RejectAsync(Guid actor, int requestId, RespondShiftSwapRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RespondAsync(actor, requestId, "REJECTED", CleanNote(request.Note), ct);
    }

    public Task<StaffShiftSwapResponse> AcceptAsync(Guid actor, int requestId, CancellationToken ct = default) =>
        RespondAsync(actor, requestId, "ACCEPTED", null, ct);

    private async Task<StaffShiftSwapResponse> RespondAsync(Guid actor, int requestId, string status, string? note, CancellationToken ct)
    {
        ValidateId(requestId);
        var staff = await GetActorAsync(actor, ct);
        var branchId = staff.BranchId!.Value;
        var reference = await FindAsync(actor, requestId, ct);
        var result = await repo.ExecuteAsync(reference.SourceAssignmentId, reference.TargetAssignmentId, requestId,
            async (source, target, swap) =>
            {
                if (swap == null) throw new KeyNotFoundException("Shift swap request not found.");
                var authorizedUser = status == "CANCELLED" ? swap.RequesterId : swap.TargetUserId;
                if (actor != authorizedUser)
                    throw new UnauthorizedAccessException(status == "CANCELLED"
                        ? "Only the requester can cancel this swap." : "Only the target staff member can respond to this swap.");
                if (swap.Status != "PENDING") throw new InvalidOperationException("Only PENDING swap requests can be changed.");
                // Revalidate the actor inside the transaction, including after a branch transfer.
                var lockedActor = await staffRepo.GetStaffAsync(actor, ct)
                    ?? throw new UnauthorizedAccessException("Staff account not found.");
                ValidateStaff(lockedActor, branchId);
                var oldValue = JsonSerializer.Serialize(Map(swap));
                if (status == "ACCEPTED")
                {
                    if (source.UserId != swap.RequesterId || target.UserId != swap.TargetUserId)
                        throw new InvalidOperationException("Assignment ownership has changed. Submit a new swap request.");
                    ValidatePair(source, target, branchId);
                    await ValidateIncomingShiftAsync(source.UserId, source.Shift, target.Shift, ct);
                    await ValidateIncomingShiftAsync(target.UserId, target.Shift, source.Shift, ct);
                    SwapOwners(actor, source, target);
                }
                swap.SourceAssignment = source;
                swap.TargetAssignment = target;
                swap.Status = status;
                swap.RespondedBy = actor;
                swap.RespondedAt = DateTime.UtcNow;
                swap.ResponseNote = note;
                swap.UpdatedAt = swap.RespondedAt.Value;
                repo.Audit(actor, status switch
                {
                    "ACCEPTED" => "ACCEPT_SHIFT_SWAP",
                    "REJECTED" => "REJECT_SHIFT_SWAP",
                    _ => "CANCEL_SHIFT_SWAP"
                }, swap, oldValue);
                return swap;
            }, ct);
        return Map(result);
    }

    private static void ValidatePair(ShiftAssignment source, ShiftAssignment target, int branchId)
    {
        if (source.UserId == target.UserId || source.ShiftId == target.ShiftId)
            throw new InvalidOperationException("A swap requires different staff members and different shifts.");
        ValidateStaff(source.User, branchId);
        ValidateStaff(target.User, branchId);
        if (!source.User.EmployeeTypeId.HasValue || source.User.EmployeeType?.Status != "ACTIVE" ||
            target.User.EmployeeType?.Status != "ACTIVE" || source.User.EmployeeTypeId != target.User.EmployeeTypeId)
            throw new InvalidOperationException("Both staff members must have the same active employee type.");
        ValidateSwappableAssignment(source, branchId);
        ValidateSwappableAssignment(target, branchId);
        // The database enforces uniqueness even for cancelled assignments; do not overwrite their history.
        if (source.Shift.ShiftAssignments.Any(a => a.UserId == target.UserId) ||
            target.Shift.ShiftAssignments.Any(a => a.UserId == source.UserId))
            throw new InvalidOperationException("A staff member already has assignment history in the incoming shift.");
    }

    private static void ValidateSwappableAssignment(ShiftAssignment assignment, int branchId)
    {
        var shift = assignment.Shift;
        if (assignment.Status != "ASSIGNED" || shift.Status != "SCHEDULED" ||
            shift.ScheduleWeek.Status is not ("OPEN_REGISTRATION" or "REGISTRATION_CLOSED") ||
            shift.ScheduleWeek.BranchId != branchId || shift.ScheduledStartAt <= DateTime.UtcNow ||
            shift.ScheduledEndAt <= shift.ScheduledStartAt)
            throw new InvalidOperationException("Only future assigned shifts in this branch can be swapped.");
        if (assignment.Attendance != null || assignment.PayslipWorkDetails.Any() ||
            assignment.LeaveRequestAssignments.Any() || assignment.TimeAdjustmentRequestDetails.Any())
            throw new InvalidOperationException("Assignments with attendance, payroll, or employee request history cannot be swapped.");
        var requirement = shift.ShiftEmployeeRequirements.SingleOrDefault(r => r.ShiftRequirementId == assignment.ShiftRequirementId);
        if (requirement == null || requirement.EmployeeTypeId != assignment.User.EmployeeTypeId ||
            requirement.EmployeeType.Status != "ACTIVE" || requirement.RequiredCount <= 0)
            throw new InvalidOperationException("The assignment has an invalid employee requirement.");
    }

    private async Task ValidateIncomingShiftAsync(Guid staffId, WorkShift outgoing, WorkShift incoming, CancellationToken ct)
    {
        var first = DateOnly.FromDateTime(incoming.ScheduledStartAt.AddHours(7));
        var last = DateOnly.FromDateTime(incoming.ScheduledEndAt.AddTicks(-1).AddHours(7));
        var others = (await assignments.GetStaffShiftsAsync(staffId, ToUtc(StartOfWeek(first)),
            ToUtc(StartOfWeek(last).AddDays(7)), ct)).Where(s => s.ShiftId != outgoing.ShiftId).ToList();
        if (others.Any(s => s.ScheduledStartAt < incoming.ScheduledEndAt && s.ScheduledEndAt > incoming.ScheduledStartAt))
            throw new InvalidOperationException("The swap creates an overlapping shift.");
        if (await assignments.HasApprovedLeaveAsync(staffId, incoming.ScheduledStartAt, incoming.ScheduledEndAt, ct))
            throw new InvalidOperationException("A staff member has approved leave during the incoming shift.");
        ValidateWorkingTime(incoming, others, await assignments.GetRulesAsync(ct));
    }

    private void SwapOwners(Guid actor, ShiftAssignment source, ShiftAssignment target)
    {
        var oldSource = AssignmentSnapshot(source);
        var oldTarget = AssignmentSnapshot(target);
        var sourceUser = source.User;
        var targetUser = target.User;
        source.UserId = targetUser.UserId;
        source.User = targetUser;
        target.UserId = sourceUser.UserId;
        target.User = sourceUser;
        var now = DateTime.UtcNow;
        foreach (var assignment in new[] { source, target })
        {
            assignment.AssignmentSource = "SHIFT_SWAP";
            assignment.AssignedBy = actor;
            assignment.AssignedAt = now;
            assignment.UpdatedAt = now;
        }
        assignments.Audit(actor, "SWAP_SHIFT_ASSIGNMENT", source, oldSource);
        assignments.Audit(actor, "SWAP_SHIFT_ASSIGNMENT", target, oldTarget);
    }

    private async Task<User> GetActorAsync(Guid actor, CancellationToken ct)
    {
        if (actor == Guid.Empty) throw new UnauthorizedAccessException("Invalid authenticated user ID.");
        var staff = await staffRepo.GetStaffAsync(actor, ct)
            ?? throw new UnauthorizedAccessException("Staff account not found.");
        ValidateStaff(staff, staff.BranchId);
        return staff;
    }

    private async Task<ShiftSwapRequest> FindAsync(Guid actor, int requestId, CancellationToken ct) =>
        await repo.FindAsync(actor, requestId, ct) ?? throw new KeyNotFoundException("Shift swap request not found.");

    private static void ValidateId(int id)
    {
        if (id <= 0) throw new ArgumentException("ID must be positive.");
    }

    private static string? CleanNote(string? note)
    {
        if (note?.Length > 500) throw new ArgumentException("Reason or response note cannot exceed 500 characters.");
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string AssignmentSnapshot(ShiftAssignment a) => JsonSerializer.Serialize(new
    {
        a.AssignmentId, a.ShiftId, a.UserId, a.ShiftRequirementId, a.AssignmentSource, a.Status, a.AssignedBy, a.AssignedAt
    });

    private static StaffShiftSwapResponse Map(ShiftSwapRequest r) => new()
    {
        SwapRequestId = r.SwapRequestId, RequesterId = r.RequesterId, RequesterName = r.Requester.FullName,
        TargetUserId = r.TargetUserId, TargetUserName = r.TargetUser.FullName,
        Reason = r.Reason, Status = r.Status, RespondedBy = r.RespondedBy, RespondedAt = r.RespondedAt,
        ResponseNote = r.ResponseNote, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
        SourceAssignment = MapAssignment(r.SourceAssignment), TargetAssignment = MapAssignment(r.TargetAssignment)
    };

    private static ShiftSwapAssignmentResponse MapAssignment(ShiftAssignment a) => new()
    {
        AssignmentId = a.AssignmentId, ShiftId = a.ShiftId, BranchId = a.Shift.ScheduleWeek.BranchId,
        ShiftName = a.Shift.ShiftNameSnapshot, ScheduledStartAt = a.Shift.ScheduledStartAt, ScheduledEndAt = a.Shift.ScheduledEndAt
    };
}
