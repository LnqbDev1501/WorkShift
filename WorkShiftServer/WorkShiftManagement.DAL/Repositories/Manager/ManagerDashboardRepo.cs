using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public sealed class ManagerDashboardRepo(AppDbContext db) : IManagerDashboardRepo
{
    public Task<User?> GetManagerAsync(Guid managerId, CancellationToken ct) => db.Users.AsNoTracking()
        .Include(x => x.Role).Include(x => x.Branch)
        .FirstOrDefaultAsync(x => x.UserId == managerId && x.DeletedAt == null, ct);

    public IQueryable<User> Staff(int branchId) => db.Users.AsNoTracking()
        .Where(x => x.BranchId == branchId && x.DeletedAt == null && x.Role.RoleName == "STAFF");
    public IQueryable<WorkShift> Shifts(int branchId) => db.WorkShifts.AsNoTracking()
        .Where(x => x.ScheduleWeek.BranchId == branchId);
    public IQueryable<ShiftAssignment> Assignments(int branchId) => db.ShiftAssignments.AsNoTracking()
        .Where(x => x.Shift.ScheduleWeek.BranchId == branchId);
    public IQueryable<Attendance> Attendances(int branchId) => db.Attendances.AsNoTracking()
        .Where(x => x.Assignment.Shift.ScheduleWeek.BranchId == branchId);
    public IQueryable<EmployeeRequest> Requests(int branchId) => db.EmployeeRequests.AsNoTracking()
        .Where(x => x.BranchId == branchId && (x.RequestType == "LEAVE" || x.RequestType == "LATE_ARRIVAL" || x.RequestType == "EARLY_LEAVE"));
    public IQueryable<WorkScheduleWeek> ScheduleWeeks(int branchId) => db.WorkScheduleWeeks.AsNoTracking()
        .Where(x => x.BranchId == branchId);
}
