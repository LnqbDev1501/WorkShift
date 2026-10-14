using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public sealed class StaffShiftSwapRepo(AppDbContext context) : IStaffShiftSwapRepo
{
    private IQueryable<ShiftSwapRequest> Details(IQueryable<ShiftSwapRequest> query) =>
        query.Include(r => r.Requester).Include(r => r.TargetUser)
            .Include(r => r.SourceAssignment).ThenInclude(a => a.Shift).ThenInclude(s => s.ScheduleWeek)
            .Include(r => r.TargetAssignment).ThenInclude(a => a.Shift).ThenInclude(s => s.ScheduleWeek);

    public async Task<(List<ShiftSwapRequest> Items, int Total)> GetPagedAsync(Guid actor, string? status,
        string direction, int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = context.ShiftSwapRequests.AsNoTracking()
            .Where(r => r.RequesterId == actor || r.TargetUserId == actor);
        if (status != null) query = query.Where(r => r.Status == status);
        if (direction == "SENT") query = query.Where(r => r.RequesterId == actor);
        if (direction == "RECEIVED") query = query.Where(r => r.TargetUserId == actor);
        var total = await query.CountAsync(ct);
        var items = await Details(query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.SwapRequestId)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return (items, total);
    }

    public Task<ShiftSwapRequest?> FindAsync(Guid actor, int requestId, CancellationToken ct) =>
        Details(context.ShiftSwapRequests.AsNoTracking().Where(r => r.SwapRequestId == requestId &&
            (r.RequesterId == actor || r.TargetUserId == actor))).SingleOrDefaultAsync(ct);

    public Task<bool> HasPendingAsync(int sourceId, int targetId, CancellationToken ct) =>
        context.ShiftSwapRequests.AnyAsync(r => r.Status == "PENDING" &&
            (r.SourceAssignmentId == sourceId || r.SourceAssignmentId == targetId ||
             r.TargetAssignmentId == sourceId || r.TargetAssignmentId == targetId), ct);

    public void Add(ShiftSwapRequest request) => context.ShiftSwapRequests.Add(request);

    public void Audit(Guid actor, string action, ShiftSwapRequest request, string? oldValue) =>
        context.AuditLogs.Add(new AuditLog
        {
            UserId = actor, Action = action, TableName = "shift_swap_requests",
            RecordId = request.SwapRequestId.ToString(), OldValue = oldValue,
            NewValue = JsonSerializer.Serialize(new
            {
                request.SwapRequestId, request.RequesterId, request.TargetUserId,
                request.SourceAssignmentId, request.TargetAssignmentId, request.Status,
                request.Reason, request.RespondedBy, request.RespondedAt, request.ResponseNote
            }), CreatedAt = DateTime.UtcNow
        });

    private IQueryable<ShiftAssignment> AssignmentDetails() => context.ShiftAssignments
        .Include(a => a.User).ThenInclude(u => u.Role)
        .Include(a => a.User).ThenInclude(u => u.Branch)
        .Include(a => a.User).ThenInclude(u => u.EmployeeType)
        .Include(a => a.Shift).ThenInclude(s => s.ScheduleWeek)
        .Include(a => a.Shift).ThenInclude(s => s.ShiftEmployeeRequirements).ThenInclude(r => r.EmployeeType)
        .Include(a => a.Shift).ThenInclude(s => s.ShiftAssignments)
        .Include(a => a.Attendance).Include(a => a.PayslipWorkDetails)
        .Include(a => a.LeaveRequestAssignments).ThenInclude(l => l.Request)
        .Include(a => a.TimeAdjustmentRequestDetails).ThenInclude(d => d.Request)
        .AsSplitQuery();

    public async Task<ShiftSwapRequest> ExecuteAsync(int sourceId, int targetId, int? requestId,
        Func<ShiftAssignment, ShiftAssignment, ShiftSwapRequest?, Task<ShiftSwapRequest>> action, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var references = await context.ShiftAssignments.AsNoTracking()
                .Where(a => a.AssignmentId == sourceId || a.AssignmentId == targetId)
                .Select(a => new { a.AssignmentId, a.UserId, a.ShiftId, a.Shift.ScheduleWeekId }).ToListAsync(ct);
            if (references.Count != 2) throw new KeyNotFoundException("Shift assignment not found.");
            // Match manager assignment lock order. Sort within each resource type to avoid reversed swap deadlocks.
            foreach (var weekId in references.Select(a => a.ScheduleWeekId).Distinct().Order())
                await context.WorkScheduleWeeks.FromSqlInterpolated(
                    $"SELECT * FROM public.work_schedule_weeks WHERE schedule_week_id = {weekId} FOR UPDATE").SingleAsync(ct);
            foreach (var userId in references.Select(a => a.UserId).Distinct().Order())
                await context.Users.FromSqlInterpolated(
                    $"SELECT * FROM public.users WHERE user_id = {userId} FOR UPDATE").SingleAsync(ct);
            foreach (var shiftId in references.Select(a => a.ShiftId).Distinct().Order())
            {
                await context.WorkShifts.FromSqlInterpolated(
                    $"SELECT * FROM public.work_shifts WHERE shift_id = {shiftId} FOR UPDATE").SingleAsync(ct);
                await context.ShiftAssignments.FromSqlInterpolated(
                    $"SELECT * FROM public.shift_assignments WHERE shift_id = {shiftId} ORDER BY assignment_id FOR UPDATE").ToListAsync(ct);
            }
            ShiftSwapRequest? request = null;
            if (requestId.HasValue)
                request = await context.ShiftSwapRequests.FromSqlInterpolated(
                    $"SELECT * FROM public.shift_swap_requests WHERE swap_request_id = {requestId.Value} FOR UPDATE")
                    .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Shift swap request not found.");
            var pair = await AssignmentDetails().Where(a => a.AssignmentId == sourceId || a.AssignmentId == targetId).ToListAsync(ct);
            if (request != null)
            {
                await context.Entry(request).Reference(r => r.Requester).LoadAsync(ct);
                await context.Entry(request).Reference(r => r.TargetUser).LoadAsync(ct);
            }
            var result = await action(pair.Single(a => a.AssignmentId == sourceId), pair.Single(a => a.AssignmentId == targetId), request);
            var isNew = context.Entry(result).State == EntityState.Added;
            await context.SaveChangesAsync(ct);
            if (isNew)
            {
                Audit(result.RequesterId, "SUBMIT_SHIFT_SWAP", result, null);
                await context.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The swap changed concurrently. Reload and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState is
            PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation or
            PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("The swap conflicts with current data. Reload and try again.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("The swap changed concurrently. Reload and try again.", ex);
        }
    }
}
