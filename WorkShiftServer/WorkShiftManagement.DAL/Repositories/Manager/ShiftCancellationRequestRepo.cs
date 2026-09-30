using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ShiftCancellationRequestRepo(AppDbContext context) : IShiftCancellationRequestRepo
{
    private IQueryable<EmployeeRequest> Scoped(int branchId) => context.EmployeeRequests
        .Where(x => x.BranchId == branchId && x.RequestType == "LEAVE" && x.LeaveRequestAssignments.Any() &&
            x.LeaveRequestAssignments.All(link => link.Assignment.UserId == x.UserId &&
                link.Assignment.Shift.ScheduleWeek.BranchId == branchId));

    private static IQueryable<EmployeeRequest> Details(IQueryable<EmployeeRequest> query) => query
        .Include(x => x.User)
        .Include(x => x.LeaveRequestAssignments).ThenInclude(x => x.Assignment)
            .ThenInclude(x => x.Shift).ThenInclude(x => x.ScheduleWeek)
        .Include(x => x.LeaveRequestAssignments).ThenInclude(x => x.Assignment)
            .ThenInclude(x => x.ShiftEmployeeRequirement!).ThenInclude(x => x.EmployeeType)
        .Include(x => x.LeaveRequestAssignments).ThenInclude(x => x.Assignment).ThenInclude(x => x.Attendance)
        .Include(x => x.LeaveRequestAssignments).ThenInclude(x => x.Assignment).ThenInclude(x => x.PayslipWorkDetails)
        .AsSplitQuery();

    public async Task<(List<EmployeeRequest> Items, int Total)> GetListAsync(
        int branchId, string? status, int page, int size, CancellationToken ct)
    {
        var query = Scoped(branchId).AsNoTracking();
        if (status != null) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(ct);
        var items = await Details(query.OrderBy(x => x.CreatedAt).ThenBy(x => x.RequestId)
            .Skip((page - 1) * size).Take(size)).ToListAsync(ct);
        return (items, total);
    }

    public Task<EmployeeRequest?> GetByIdAsync(int id, int branchId, CancellationToken ct) =>
        Details(Scoped(branchId).AsNoTracking()).FirstOrDefaultAsync(x => x.RequestId == id, ct);

    public async Task<EmployeeRequest> ReviewAsync(int id, int branchId, Action<EmployeeRequest> review, CancellationToken ct)
    {
        // A serializable transaction also detects changes to links, shifts and attendance
        // between validation and commit. Locks serialize competing reviews of an assignment.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var request = await context.EmployeeRequests.FromSqlInterpolated(
                $"SELECT * FROM public.employee_requests WHERE request_id = {id} AND branch_id = {branchId} AND request_type = 'LEAVE' FOR UPDATE")
                .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Shift cancellation request not found.");

            await context.ShiftAssignments.FromSqlInterpolated(
                $"SELECT a.* FROM public.shift_assignments a JOIN public.leave_request_assignments l ON l.assignment_id = a.assignment_id WHERE l.request_id = {id} ORDER BY a.assignment_id FOR UPDATE OF a")
                .ToListAsync(ct);

            request = await Details(Scoped(branchId)).SingleOrDefaultAsync(x => x.RequestId == id, ct)
                ?? throw new KeyNotFoundException("Shift cancellation request not found.");
            review(request);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return request;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The request changed concurrently. Reload and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
            pg.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
                or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation)
        {
            throw new InvalidOperationException("The request conflicts with current data. Reload and try again.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("The request changed concurrently. Reload and try again.", ex);
        }
    }
}
