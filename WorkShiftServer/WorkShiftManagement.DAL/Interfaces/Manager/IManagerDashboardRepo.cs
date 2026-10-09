using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IManagerDashboardRepo
{
    Task<User?> GetManagerAsync(Guid managerId, CancellationToken ct);
    IQueryable<User> Staff(int branchId);
    IQueryable<WorkShift> Shifts(int branchId);
    IQueryable<ShiftAssignment> Assignments(int branchId);
    IQueryable<Attendance> Attendances(int branchId);
    IQueryable<EmployeeRequest> Requests(int branchId);
    IQueryable<WorkScheduleWeek> ScheduleWeeks(int branchId);
}
