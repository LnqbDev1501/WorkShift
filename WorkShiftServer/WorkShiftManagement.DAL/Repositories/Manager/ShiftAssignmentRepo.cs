using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Version.Rules;
using WorkShiftManagement.DAL.Notifications;
namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ShiftAssignmentRepo(AppDbContext context) : IShiftAssignmentRepo
{
    public Task<ShiftAssignment?> FindAsync(int assignmentId, int branchId, CancellationToken ct) =>
        context.ShiftAssignments.AsNoTracking().FirstOrDefaultAsync(x =>
            x.AssignmentId == assignmentId && x.Shift.ScheduleWeek.BranchId == branchId, ct);

    public Task<List<WorkShift>> GetStaffShiftsAsync(Guid staffId, DateTime from, DateTime to, CancellationToken ct) =>
        context.WorkShifts.AsNoTracking().Where(s => s.Status != "CANCELLED" &&
            s.ScheduledStartAt < to && s.ScheduledEndAt > from &&
            s.ShiftAssignments.Any(a => a.UserId == staffId && (a.Status == "ASSIGNED" || a.Status == "COMPLETED")))
            .ToListAsync(ct);

    public Task<List<WorkingTimeRule>> GetRulesAsync(CancellationToken ct) =>
        context.WorkingTimeRules.AsNoTracking()
            .Where(r => r.Status == WorkingTimeRuleLifecycle.Active ||
                r.Status == WorkingTimeRuleLifecycle.Published ||
                r.Status == WorkingTimeRuleLifecycle.Inactive).ToListAsync(ct);

    public void Add(ShiftAssignment assignment) => context.ShiftAssignments.Add(assignment);

    public void AddNotification(WorkShiftManagement.DAL.Entities.Notification notification)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Assignment notifications must be saved in the assignment transaction.");
        context.Notifications.Add(notification);
    }

    public async Task CancelPendingSwapsAsync(int assignmentId, Guid actor, DateTime cancelledAt,
        string reason, CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Swap cancellation must run inside the assignment transaction.");

        // The conditional update locks matching request rows and never overwrites a
        // terminal outcome. A conflicting concurrent transition causes the outer
        // serializable transaction to roll back instead of partially cancelling.
        var pending = await context.ShiftSwapRequests
            .Include(r => r.SourceAssignment).ThenInclude(a => a.Shift)
            .Include(r => r.TargetAssignment).ThenInclude(a => a.Shift)
            .Where(r => r.Status == "PENDING" &&
                (r.SourceAssignmentId == assignmentId || r.TargetAssignmentId == assignmentId))
            .AsNoTracking().ToListAsync(ct);
        var affected = await context.ShiftSwapRequests
            .Where(request => request.Status == "PENDING" &&
                (request.SourceAssignmentId == assignmentId || request.TargetAssignmentId == assignmentId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, "CANCELLED")
                .SetProperty(request => request.RespondedBy, actor)
                .SetProperty(request => request.RespondedAt, cancelledAt)
                .SetProperty(request => request.ResponseNote, reason)
                .SetProperty(request => request.UpdatedAt, cancelledAt), ct);
        if (affected != pending.Count)
            throw new InvalidOperationException("Pending swaps changed concurrently. Reload and try again.");
        foreach (var swap in pending)
        {
            swap.Status = "CANCELLED";
            swap.ResponseNote = $"Assignment cancelled by manager. Reason: {reason}";
            context.Notifications.Add(ScheduleNotificationFactory.SwapChanged(swap, actor, cancelledAt));
        }
    }

    public Task<bool> HasApprovedLeaveAsync(Guid staffId, DateTime start, DateTime end, CancellationToken ct) =>
        context.LeaveRequestAssignments.AnyAsync(l => l.Request.UserId == staffId &&
            l.Request.RequestType == "LEAVE" && l.Request.Status == "APPROVED" &&
            l.Assignment.Shift.ScheduledStartAt < end && l.Assignment.Shift.ScheduledEndAt > start, ct);

    public void Audit(Guid actor, string action, ShiftAssignment assignment, string? oldValue) =>
        context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            Action = action,
            TableName = "shift_assignments",
            RecordId = assignment.AssignmentId.ToString(),
            OldValue = oldValue,
            NewValue = Snapshot(assignment),
            CreatedAt = DateTime.UtcNow
        });

    public static string Snapshot(ShiftAssignment a) => JsonSerializer.Serialize(new
    {
        a.AssignmentId,
        a.ShiftId,
        a.UserId,
        a.ShiftRequirementId,
        a.Status,
        a.AssignmentSource,
        a.AssignedBy,
        a.AssignedAt,
        a.CancelledBy,
        a.CancelledAt,
        a.CancelReason
    });

    public async Task<ShiftAssignment> ExecuteAsync(int shiftId, Guid staffId, int branchId,
        Func<WorkShift, User, Task<ShiftAssignment>> action, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var weekId = await context.WorkShifts.Where(s => s.ShiftId == shiftId && s.ScheduleWeek.BranchId == branchId)
                .Select(s => (int?)s.ScheduleWeekId).SingleOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Shift not found.");
            await context.WorkScheduleWeeks.FromSqlInterpolated(
                $"SELECT * FROM public.work_schedule_weeks WHERE schedule_week_id = {weekId} AND branch_id = {branchId} FOR UPDATE").SingleAsync(ct);
            // Lock both staff and shift: protect overlapping assignments across shifts and the last slot.
            var staff = await context.Users.FromSqlInterpolated(
                $"SELECT * FROM public.users WHERE user_id = {staffId} FOR UPDATE").SingleOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Staff not found.");
            await context.Entry(staff).Reference(x => x.Role).LoadAsync(ct);
            await context.Entry(staff).Reference(x => x.Branch).LoadAsync(ct);
            await context.Entry(staff).Reference(x => x.EmployeeType).LoadAsync(ct);
            await context.WorkShifts.FromSqlInterpolated(
                $"SELECT * FROM public.work_shifts WHERE shift_id = {shiftId} FOR UPDATE").SingleAsync(ct);
            await context.ShiftAssignments.FromSqlInterpolated(
                $"SELECT * FROM public.shift_assignments WHERE shift_id = {shiftId} ORDER BY assignment_id FOR UPDATE").ToListAsync(ct);
            var shift = await context.WorkShifts
                .Include(x => x.ScheduleWeek)
                .Include(x => x.ShiftEmployeeRequirements).ThenInclude(x => x.EmployeeType)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.Attendance)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.PayslipWorkDetails)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.LeaveRequestAssignments).ThenInclude(x => x.Request)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.TimeAdjustmentRequestDetails).ThenInclude(x => x.Request)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.ShiftSwapRequestSourceAssignments)
                .Include(x => x.ShiftAssignments).ThenInclude(x => x.ShiftSwapRequestTargetAssignments)
                .AsSplitQuery().SingleAsync(x => x.ShiftId == shiftId, ct);
            var result = await action(shift, staff);
            var isNew = context.Entry(result).State == EntityState.Added;
            await context.SaveChangesAsync(ct);
            if (isNew)
            {
                Audit(result.AssignedBy, "ASSIGN_SHIFT", result, null);
                await context.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Assignment changed concurrently. Reload and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState is
            PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation
            or PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("Assignment conflicts with current data. Reload and try again.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("Assignment changed concurrently. Reload and try again.", ex);
        }
    }
}
