using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using static WorkShiftManagement.BLL.Helpers.ShiftAssignmentRules;
namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ShiftAssignmentService(IShiftAssignmentRepo repo, IScheduleWeekRepo weekRepo,
    INotificationRealtimePublisher realtimePublisher, ILogger<ShiftAssignmentService> logger) : IShiftAssignmentService
{
    public async Task<ShiftAssignmentResponse> AssignAsync(Guid actor, int shiftId, AssignShiftRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (shiftId <= 0 || request.UserId == Guid.Empty)
            throw new ArgumentException("A positive shift ID and valid staff ID are required.");
        var branchId = await GetBranchAsync(actor);
        WorkShiftManagement.DAL.Entities.Notification? notification = null;
        var result = await repo.ExecuteAsync(shiftId, request.UserId, branchId, async (shift, staff) =>
        {
            ValidateShift(shift);
            ValidateAssignmentDeadline(shift.ScheduledStartAt, DateTime.UtcNow);
            if (staff.BranchId != branchId) throw new KeyNotFoundException("Staff not found in this branch.");
            if (staff.Status != "ACTIVE" || staff.DeletedAt.HasValue ||
                staff.Role.RoleName != "STAFF" || staff.Role.Status != "ACTIVE")
                throw new InvalidOperationException("Only active staff in an active STAFF role can be assigned.");
            if (!staff.EmployeeTypeId.HasValue || staff.EmployeeType?.Status != "ACTIVE")
                throw new InvalidOperationException("Staff must have an active employee type.");
            var requirement = shift.ShiftEmployeeRequirements.SingleOrDefault(r => r.EmployeeTypeId == staff.EmployeeTypeId)
                ?? throw new InvalidOperationException("The shift does not require this employee type.");
            if (requirement.RequiredCount <= 0 || requirement.EmployeeType.Status != "ACTIVE")
                throw new InvalidOperationException("The employee requirement is not valid.");
            var existing = shift.ShiftAssignments.SingleOrDefault(a => a.UserId == staff.UserId);
            if (existing != null && existing.Status != "CANCELLED")
                throw new InvalidOperationException("Staff is already assigned to this shift.");
            if (existing != null) ValidateHistory(existing, restoring: true);
            ValidateCapacity(shift, requirement);

            var firstDay = DateOnly.FromDateTime(shift.ScheduledStartAt.AddHours(7));
            var lastDay = DateOnly.FromDateTime(shift.ScheduledEndAt.AddTicks(-1).AddHours(7));
            var from = StartOfWeek(firstDay);
            var to = StartOfWeek(lastDay).AddDays(7);
            var otherShifts = await repo.GetStaffShiftsAsync(staff.UserId, ToUtc(from), ToUtc(to), ct);
            if (otherShifts.Any(s => s.ScheduledStartAt < shift.ScheduledEndAt && s.ScheduledEndAt > shift.ScheduledStartAt))
                throw new InvalidOperationException("Staff has an overlapping shift.");
            if (await repo.HasApprovedLeaveAsync(staff.UserId, shift.ScheduledStartAt, shift.ScheduledEndAt, ct))
                throw new InvalidOperationException("Staff has approved leave during this shift.");
            ValidateWorkingTime(shift, otherShifts, await repo.GetRulesAsync(ct));

            var now = DateTime.UtcNow;
            var assignment = existing ?? new ShiftAssignment
            {
                ShiftId = shift.ShiftId, UserId = staff.UserId, CreatedAt = now
            };
            var oldValue = existing == null ? null : JsonSerializer.Serialize(Map(existing));
            assignment.ShiftRequirementId = requirement.ShiftRequirementId;
            assignment.AssignmentSource = "MANAGER_ASSIGNMENT";
            assignment.AssignedBy = actor;
            assignment.AssignedAt = now;
            assignment.Status = "ASSIGNED";
            assignment.CancelledBy = null;
            assignment.CancelledAt = null;
            assignment.CancelReason = null;
            assignment.UpdatedAt = now;
            if (existing == null) repo.Add(assignment);
            else repo.Audit(actor, "RESTORE_SHIFT_ASSIGNMENT", assignment, oldValue);
            notification = QueueAssignmentNotification(actor, staff.UserId, shift, now, cancelled: false);
            return assignment;
        }, ct);
        await PublishNotificationAsync(notification!);
        return Map(result);
    }

    public async Task<ShiftAssignmentResponse> CancelAsync(Guid actor, int assignmentId,
        CancelShiftAssignmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (assignmentId <= 0) throw new ArgumentException("Assignment ID must be positive.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ArgumentException("Cancellation reason must contain 1 to 500 characters.");
        var branchId = await GetBranchAsync(actor);
        var reference = await repo.FindAsync(assignmentId, branchId, ct)
            ?? throw new KeyNotFoundException("Assignment not found.");
        WorkShiftManagement.DAL.Entities.Notification? notification = null;
        var result = await repo.ExecuteAsync(reference.ShiftId, reference.UserId, branchId, async (shift, _) =>
        {
            ValidateShift(shift);
            var assignment = shift.ShiftAssignments.SingleOrDefault(a => a.AssignmentId == assignmentId && a.UserId == reference.UserId)
                ?? throw new KeyNotFoundException("Assignment not found.");
            if (assignment.Status != "ASSIGNED")
                throw new InvalidOperationException("Only ASSIGNED assignments can be cancelled.");
            ValidateHistory(assignment, restoring: false);
            var oldValue = JsonSerializer.Serialize(Map(assignment));
            var cancelledAt = DateTime.UtcNow;
            var reason = request.Reason.Trim();
            await repo.CancelPendingSwapsAsync(assignmentId, actor, cancelledAt, reason, ct);
            assignment.Status = "CANCELLED";
            assignment.CancelledBy = actor;
            assignment.CancelledAt = cancelledAt;
            assignment.CancelReason = reason;
            assignment.UpdatedAt = assignment.CancelledAt.Value;
            repo.Audit(actor, "CANCEL_SHIFT_ASSIGNMENT", assignment, oldValue);
            notification = QueueAssignmentNotification(actor, assignment.UserId, shift, cancelledAt,
                cancelled: true, reason);
            return assignment;
        }, ct);
        await PublishNotificationAsync(notification!);
        return Map(result);
    }

    private WorkShiftManagement.DAL.Entities.Notification QueueAssignmentNotification(Guid actor, Guid recipient, WorkShift shift,
        DateTime timestamp, bool cancelled, string? reason = null)
    {
        var start = shift.ScheduledStartAt.AddHours(7).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        var end = shift.ScheduledEndAt.AddHours(7).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        var notification = new WorkShiftManagement.DAL.Entities.Notification
        {
            Title = cancelled ? "Shift assignment cancelled" : "New shift assignment",
            Content = $"Your assignment to {shift.ShiftNameSnapshot} (shift #{shift.ShiftId}), {start}–{end} (UTC+7), " +
                (cancelled ? $"has been cancelled by your manager. Reason: {reason}" : "has been confirmed by your manager."),
            NotificationType = "ShiftAssignment",
            CreatedBy = actor, CreatedAt = timestamp, UpdatedAt = timestamp,
            TargetType = "Custom", Channels = ["InApp"], TargetRoles = ["STAFF"],
            UserNotifications = [new UserNotification { UserId = recipient, CreatedAt = timestamp }]
        };
        // Persist the recipient's inbox item atomically with the assignment.
        repo.AddNotification(notification);
        return notification;
    }

    private async Task PublishNotificationAsync(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        try
        {
            await realtimePublisher.PublishAsync(notification.UserNotifications.Single().UserId,
                new RealtimeNotificationResponse
                {
                    NotificationId = notification.NotificationId, Title = notification.Title,
                    Message = notification.Content, CreatedAt = notification.CreatedAt
                });
        }
        catch (Exception exception)
        {
            // The transaction has committed and the inbox item is durable. A
            // realtime failure must not report the assignment as unsuccessful.
            logger.LogWarning(exception, "Realtime delivery failed for assignment notification {NotificationId}.",
                notification.NotificationId);
        }
    }

    private async Task<int> GetBranchAsync(Guid actor)
    {
        if (actor == Guid.Empty) throw new UnauthorizedAccessException("Invalid authenticated user ID.");
        var manager = await weekRepo.GetActiveManagerAsync(actor)
            ?? throw new UnauthorizedAccessException("An active manager is required.");
        if (manager.BranchId is not int branchId || manager.Branch?.Status != "ACTIVE" || manager.Role.Status != "ACTIVE")
            throw new UnauthorizedAccessException("An active manager role and branch are required.");
        return branchId;
    }

    internal static void ValidateAssignmentDeadline(DateTime scheduledStartAt, DateTime utcNow)
    {
        // A Thursday shift closes assignment at Wednesday 00:00 in Vietnam.
        var deadlineUtc = DateTime.SpecifyKind(scheduledStartAt.AddHours(7).Date.AddDays(-1).AddHours(-7), DateTimeKind.Utc);
        if (utcNow >= deadlineUtc)
            throw new InvalidOperationException("Employees must be assigned before 00:00 on the day before the shift (UTC+7).");
    }

    private static void ValidateShift(WorkShift shift)
    {
        if (shift.Status != "SCHEDULED" || shift.ScheduleWeek.Status is not ("OPEN_REGISTRATION" or "REGISTRATION_CLOSED"))
            throw new InvalidOperationException("Assignments can only be changed in a scheduled shift in an opened or closed registration week.");
        if (shift.ScheduledStartAt <= DateTime.UtcNow || shift.ScheduledEndAt <= shift.ScheduledStartAt)
            throw new InvalidOperationException("The shift has started or has invalid times.");
    }

    private static ShiftAssignmentResponse Map(ShiftAssignment a) => new()
    {
        AssignmentId = a.AssignmentId, ShiftId = a.ShiftId, UserId = a.UserId,
        ShiftRequirementId = a.ShiftRequirementId, Status = a.Status, AssignmentSource = a.AssignmentSource,
        AssignedBy = a.AssignedBy, AssignedAt = a.AssignedAt,
        CancelledBy = a.CancelledBy, CancelledAt = a.CancelledAt, CancelReason = a.CancelReason
    };
}
