using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IWorkingTimeRuleRepo
{
    Task<List<WorkingTimeRule>> GetAllAsync(CancellationToken cancellationToken);

    Task<WorkingTimeRule> CreateVersionAsync(WorkingTimeRule rule, CancellationToken cancellationToken);
    Task<WorkingTimeRule?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRule> UpdateDraftAsync(int id, WorkingTimeRule values, CancellationToken cancellationToken);
    Task DeleteDraftAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRule> PublishAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRule> CancelAsync(int id, CancellationToken cancellationToken);
    Task ActivateDueAsync(CancellationToken cancellationToken);
}
