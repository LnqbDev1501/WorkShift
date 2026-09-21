using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IWorkingTimeRuleRepo
{
    Task<List<WorkingTimeRule>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates the active version and deactivates previous versions atomically,
    /// serializing concurrent creation requests.
    /// </summary>
    Task<WorkingTimeRule> CreateVersionAsync(WorkingTimeRule rule, CancellationToken cancellationToken);
}
