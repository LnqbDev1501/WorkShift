using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IShiftRepo
{
    Task<WorkShift?> GetByIdAsync(int shiftId, int branchId, CancellationToken ct);
    Task<WorkShift?> GetForUpdateAsync(int shiftId, int branchId, CancellationToken ct);
    Task<ShiftTemplate?> GetTemplateAsync(int templateId, CancellationToken ct);
    Task<List<EmployeeType>> GetEmployeeTypesAsync(IEnumerable<int> ids, CancellationToken ct);
    Task<bool> ExistsAsync(int weekId, DateTime start, DateTime end, int? excludeId, CancellationToken ct);
    // The callback and SaveChanges run inside a transaction holding the parent week lock.
    Task ExecuteDraftAsync(int weekId, int branchId, Func<WorkScheduleWeek, Task> action, CancellationToken ct);
    Task<bool> HasDependenciesAsync(int shiftId, CancellationToken ct);
    void Add(WorkShift shift);
    void Remove(WorkShift shift);
    void RemoveRequirements(IEnumerable<ShiftEmployeeRequirement> requirements);
}
