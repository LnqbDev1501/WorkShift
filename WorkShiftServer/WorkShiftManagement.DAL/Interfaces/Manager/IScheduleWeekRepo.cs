using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IScheduleWeekRepo
{
    Task<User?> GetActiveManagerAsync(Guid managerId);
    Task<List<WorkScheduleWeek>> GetListAsync(int branchId, DateOnly? fromDate, DateOnly? toDate, string? status);
    Task<WorkScheduleWeek?> GetDetailAsync(int scheduleWeekId, int branchId, bool tracking = false);
    Task<bool> ExistsAsync(int branchId, DateOnly weekStartDate, int? excludeId = null);
    Task<bool> HasShiftsAsync(int scheduleWeekId);
    Task<bool> HasDeleteBlockingDependenciesAsync(int scheduleWeekId);
    Task AddAsync(WorkScheduleWeek scheduleWeek);
    Task SaveChangesAsync();
    Task DeleteDraftAsync(WorkScheduleWeek scheduleWeek);
    Task<bool> OpenRegistrationAsync(int scheduleWeekId, int branchId, DateTime openedAt);
    Task<bool> CloseRegistrationAsync(int scheduleWeekId, int branchId, DateTime closedAt);
}
