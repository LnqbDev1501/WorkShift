using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public sealed class StaffShiftRepo(AppDbContext context) : IStaffShiftRepo
{
    public Task<User?> GetStaffAsync(Guid staffId, CancellationToken ct) =>
        context.Users.AsNoTracking().Include(x => x.Role).Include(x => x.Branch)
            .Include(x => x.EmployeeType).SingleOrDefaultAsync(x => x.UserId == staffId, ct);

    private IQueryable<WorkShift> WithDetails(IQueryable<WorkShift> query, Guid staffId) =>
        query.AsNoTracking().Include(x => x.ScheduleWeek).ThenInclude(x => x.Branch)
            .Include(x => x.ShiftEmployeeRequirements).ThenInclude(x => x.EmployeeType)
            .Include(x => x.ShiftAssignments.Where(a => a.UserId == staffId ||
                a.Status == "ASSIGNED" || a.Status == "COMPLETED")).AsSplitQuery();

    public async Task<(List<WorkShift> Items, int Total)> GetPagedAsync(Guid staffId, int branchId,
        int? employeeTypeId, bool availableOnly, DateTime? from, DateTime? to,
        int pageNumber, int pageSize, DateTime now, CancellationToken ct)
    {
        var query = context.WorkShifts.Where(s => s.ScheduleWeek.Status != "DRAFT");
        if (availableOnly)
        {
            query = query.Where(s => s.ScheduleWeek.BranchId == branchId && s.Status == "SCHEDULED" &&
                s.ScheduleWeek.Status == "OPEN_REGISTRATION" && s.ScheduleWeek.RegistrationDeadline > now &&
                s.ScheduledStartAt > now && s.ScheduledEndAt > s.ScheduledStartAt &&
                !s.ShiftAssignments.Any(a => a.UserId == staffId && a.Status != "CANCELLED") &&
                s.ShiftEmployeeRequirements.Any(r => r.EmployeeTypeId == employeeTypeId &&
                    r.EmployeeType.Status == "ACTIVE" && r.RequiredCount >
                    s.ShiftAssignments.Count(a => a.ShiftRequirementId == r.ShiftRequirementId &&
                        (a.Status == "ASSIGNED" || a.Status == "COMPLETED"))));
        }
        else
        {
            // Preserve the employee's own schedule after a branch transfer.
            query = query.Where(s => s.ShiftAssignments.Any(a => a.UserId == staffId &&
                (a.Status == "ASSIGNED" || a.Status == "COMPLETED")));
        }
        if (from.HasValue) query = query.Where(s => s.ScheduledStartAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.ScheduledStartAt < to.Value);
        var total = await query.CountAsync(ct);
        var items = await WithDetails(query.OrderBy(s => s.ScheduledStartAt).ThenBy(s => s.ShiftId)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize), staffId).ToListAsync(ct);
        return (items, total);
    }

    public Task<WorkShift?> GetDetailAsync(int shiftId, Guid staffId, int branchId, CancellationToken ct) =>
        WithDetails(context.WorkShifts.Where(s => s.ShiftId == shiftId && s.ScheduleWeek.Status != "DRAFT" &&
            (s.ScheduleWeek.BranchId == branchId || s.ShiftAssignments.Any(a => a.UserId == staffId))), staffId)
            .SingleOrDefaultAsync(ct);
}
