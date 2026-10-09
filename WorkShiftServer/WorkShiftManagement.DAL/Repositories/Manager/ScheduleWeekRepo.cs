using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ScheduleWeekRepo(AppDbContext context) : IScheduleWeekRepo
{
    public Task<User?> GetActiveManagerAsync(Guid managerId) => context.Users
        .AsNoTracking()
        .Include(user => user.Branch)
        .Include(user => user.Role)
        .FirstOrDefaultAsync(user =>
            user.UserId == managerId &&
            user.Status == "ACTIVE" &&
            user.Role.RoleName == "MANAGER");

    public Task<List<WorkScheduleWeek>> GetListAsync(
        int branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status)
    {
        var query = DetailQuery()
            .Where(week => week.BranchId == branchId);

        if (fromDate.HasValue)
            query = query.Where(week => week.WeekEndDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(week => week.WeekStartDate <= toDate.Value);

        if (status != null)
            query = query.Where(week => week.Status == status);

        return query
            .OrderByDescending(week => week.WeekStartDate)
            .ThenByDescending(week => week.ScheduleWeekId)
            .ToListAsync();
    }

    public Task<WorkScheduleWeek?> GetDetailAsync(
        int scheduleWeekId,
        int branchId,
        bool tracking = false)
    {
        var query = DetailQuery(tracking);
        return query.FirstOrDefaultAsync(week =>
            week.ScheduleWeekId == scheduleWeekId &&
            week.BranchId == branchId);
    }

    public Task<bool> ExistsAsync(int branchId, DateOnly weekStartDate, int? excludeId = null) =>
        context.WorkScheduleWeeks.AnyAsync(week =>
            week.BranchId == branchId &&
            week.WeekStartDate == weekStartDate &&
            (!excludeId.HasValue || week.ScheduleWeekId != excludeId.Value));

    public Task<bool> HasShiftsAsync(int scheduleWeekId) =>
        context.WorkShifts.AnyAsync(shift => shift.ScheduleWeekId == scheduleWeekId);

    public async Task<bool> HasDeleteBlockingDependenciesAsync(int scheduleWeekId)
    {
        if (await context.PayrollPeriods.AnyAsync(period => period.ScheduleWeekId == scheduleWeekId))
            return true;

        return await context.WorkShifts
            .Where(shift => shift.ScheduleWeekId == scheduleWeekId)
            .AnyAsync(shift =>
                shift.ShiftAssignments.Any() ||
                shift.Checklists.Any());
    }

    public async Task AddAsync(WorkScheduleWeek scheduleWeek)
    {
        context.WorkScheduleWeeks.Add(scheduleWeek);
        await SaveChangesAsync();
    }

    public Task SaveChangesAsync() => SaveWithConstraintHandlingAsync();

    public async Task DeleteDraftAsync(WorkScheduleWeek scheduleWeek)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.WorkShifts.RemoveRange(scheduleWeek.WorkShifts);
        context.WorkScheduleWeeks.Remove(scheduleWeek);
        await SaveWithConstraintHandlingAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> OpenRegistrationAsync(int scheduleWeekId, int branchId, DateTime openedAt)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var affectedWeeks = await context.WorkScheduleWeeks
            .Where(week =>
                week.ScheduleWeekId == scheduleWeekId &&
                week.BranchId == branchId &&
                week.Status == "DRAFT")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(week => week.Status, "OPEN_REGISTRATION")
                .SetProperty(week => week.OpenedAt, openedAt)
                .SetProperty(week => week.UpdatedAt, openedAt));

        if (affectedWeeks == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        // Recheck after acquiring the week row lock: a draft shift may have been
        // deleted or edited while the service was validating the publication.
        var shifts = context.WorkShifts.Where(shift => shift.ScheduleWeekId == scheduleWeekId);
        if (!await shifts.AnyAsync() ||
            await shifts.AnyAsync(shift => shift.Status != "DRAFT" ||
                !shift.ShiftEmployeeRequirements.Any() ||
                shift.ShiftEmployeeRequirements.Any(requirement =>
                    requirement.RequiredCount <= 0 || requirement.EmployeeType.Status != "ACTIVE")))
        {
            throw new InvalidOperationException(
                "The draft shifts changed or contain invalid staffing requirements. Reload the schedule before opening registration.");
        }

        await context.WorkShifts
            .Where(shift => shift.ScheduleWeekId == scheduleWeekId && shift.Status == "DRAFT")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(shift => shift.Status, "SCHEDULED")
                .SetProperty(shift => shift.UpdatedAt, openedAt));

        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> CloseRegistrationAsync(int scheduleWeekId, int branchId, DateTime closedAt)
    {
        var affectedWeeks = await context.WorkScheduleWeeks
            .Where(week =>
                week.ScheduleWeekId == scheduleWeekId &&
                week.BranchId == branchId &&
                week.Status == "OPEN_REGISTRATION")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(week => week.Status, "REGISTRATION_CLOSED")
                .SetProperty(week => week.RegistrationClosedAt, closedAt)
                .SetProperty(week => week.UpdatedAt, closedAt));

        return affectedWeeks == 1;
    }

    private IQueryable<WorkScheduleWeek> DetailQuery(bool tracking = false)
    {
        var query = context.WorkScheduleWeeks
            .Include(week => week.WorkShifts)
                .ThenInclude(shift => shift.ShiftEmployeeRequirements)
                    .ThenInclude(requirement => requirement.EmployeeType)
            .Include(week => week.WorkShifts)
                .ThenInclude(shift => shift.ShiftAssignments)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    private async Task SaveWithConstraintHandlingAsync()
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_schedule_week_branch_start"
            })
        {
            throw new InvalidOperationException(
                "A work schedule already exists for this branch and week.", exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation
            })
        {
            throw new InvalidOperationException(
                "The work schedule is referenced by other data and cannot be deleted.", exception);
        }
    }
}
