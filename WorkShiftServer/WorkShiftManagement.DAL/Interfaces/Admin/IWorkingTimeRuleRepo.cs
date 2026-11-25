using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IWorkingTimeRuleRepo
{
    Task<List<WorkingTimeRule>> GetAllAsync(CancellationToken cancellationToken);

    Task<WorkingTimeRule> CreateVersionAsync(WorkingTimeRule rule, CancellationToken cancellationToken);
    Task<WorkingTimeRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRule> UpdateDraftAsync(int id, WorkingTimeRule values, CancellationToken cancellationToken);
    Task DeleteDraftAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRule> PublishAsync(int id, Guid actor, CancellationToken cancellationToken);
    Task<WorkingTimeRule> CancelAsync(int id, Guid actor, CancellationToken cancellationToken);
    Task<int> ActivateDueAsync(CancellationToken cancellationToken);
}
