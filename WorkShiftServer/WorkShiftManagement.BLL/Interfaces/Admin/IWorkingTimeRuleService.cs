using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IWorkingTimeRuleService
{
    Task<List<WorkingTimeRuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> CreateAsync(WorkingTimeRuleRequest request, Guid createdBy, CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> UpdateAsync(int id, WorkingTimeRuleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> PublishAsync(int id, CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> CancelAsync(int id, CancellationToken cancellationToken);
    Task ActivateDueAsync(CancellationToken cancellationToken);
}
