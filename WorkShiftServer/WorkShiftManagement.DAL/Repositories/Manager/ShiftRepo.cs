using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Policies.Shifts;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ShiftRepo(AppDbContext context) : IShiftRepo
{
    public Task<List<ShiftTemplate>> GetActiveTemplatesAsync(CancellationToken ct) => context.ShiftTemplates.AsNoTracking()
        .Where(x => x.Status == "ACTIVE").OrderBy(x => x.ShiftName).ToListAsync(ct);
    public Task<List<EmployeeType>> GetActiveTypesAsync(CancellationToken ct) => context.EmployeeTypes.AsNoTracking()
        .Where(x => x.Status == "ACTIVE").OrderBy(x => x.TypeName).ToListAsync(ct);
    public Task<List<User>> GetActiveStaffAsync(int branchId, CancellationToken ct)
    {
        var contracts = StaffShiftContractPolicy.ActiveContracts(context.EmploymentContracts, branchId, DateTime.UtcNow);
        return context.Users.AsNoTracking().Include(x => x.EmployeeType)
            .Where(x => x.BranchId == branchId && x.DeletedAt == null && x.Status == "ACTIVE" && x.Role.RoleName == "STAFF" &&
                x.Role.Status == "ACTIVE" && x.EmployeeType != null && x.EmployeeType.Status == "ACTIVE" &&
                contracts.Any(c => c.UserId == x.UserId))
            .OrderBy(x => x.FullName).ThenBy(x => x.UserId).ToListAsync(ct);
    }
    public Task<WorkShift?> GetForUpdateAsync(int shiftId, int branchId, CancellationToken ct) =>
        context.WorkShifts.Include(x => x.ShiftEmployeeRequirements)
            .FirstOrDefaultAsync(x => x.ShiftId == shiftId && x.ScheduleWeek.BranchId == branchId, ct);

    public Task<WorkShift?> GetByIdAsync(int shiftId, int branchId, CancellationToken ct) =>
        context.WorkShifts.AsNoTracking()
            .Include(x => x.ShiftEmployeeRequirements).ThenInclude(x => x.EmployeeType)
            .Include(x => x.ShiftAssignments).ThenInclude(x => x.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.ShiftId == shiftId && x.ScheduleWeek.BranchId == branchId, ct);

    public Task<ShiftTemplate?> GetTemplateAsync(int templateId, CancellationToken ct) =>
        context.ShiftTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.ShiftTemplateId == templateId, ct);

    public Task<List<EmployeeType>> GetEmployeeTypesAsync(IEnumerable<int> ids, CancellationToken ct) =>
        context.EmployeeTypes.AsNoTracking().Where(x => ids.Contains(x.EmployeeTypeId)).ToListAsync(ct);

    public Task<bool> ExistsAsync(int weekId, DateTime start, DateTime end, int? excludeId, CancellationToken ct) =>
        context.WorkShifts.AnyAsync(x => x.ScheduleWeekId == weekId && x.Status != "CANCELLED" &&
            x.ScheduledStartAt == start && x.ScheduledEndAt == end &&
            (!excludeId.HasValue || x.ShiftId != excludeId.Value), ct);

    public Task<bool> HasDependenciesAsync(int shiftId, CancellationToken ct) =>
        context.WorkShifts.AnyAsync(x => x.ShiftId == shiftId &&
            (x.ShiftAssignments.Any() || x.Checklists.Any()), ct);

    public void Add(WorkShift shift) => context.WorkShifts.Add(shift);
    public void Remove(WorkShift shift) => context.WorkShifts.Remove(shift);
    public void RemoveRequirements(IEnumerable<ShiftEmployeeRequirement> requirements) =>
        context.ShiftEmployeeRequirements.RemoveRange(requirements);

    public async Task ExecuteDraftAsync(int weekId, int branchId, Func<WorkScheduleWeek, Task> action, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            // Parameterized SQL: serialize shift edits with other edits and week publication.
            var week = await context.WorkScheduleWeeks
                .FromSqlInterpolated($"SELECT * FROM public.work_schedule_weeks WHERE schedule_week_id = {weekId} AND branch_id = {branchId} FOR UPDATE")
                .SingleOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Work schedule not found.");
            if (week.Status != "DRAFT" || week.OpenedAt.HasValue || week.RegistrationClosedAt.HasValue)
                throw new InvalidOperationException("Shifts can only be changed in a work week that has never opened registration.");
            await action(week);
            // Also make concurrent readers using optimistic checks observe the change.
            week.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The schedule changed concurrently. Reload and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation })
        {
            throw new InvalidOperationException("The shift conflicts with existing or related data. Reload and try again.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("The schedule changed concurrently. Reload and try again.", ex);
        }
    }
}
