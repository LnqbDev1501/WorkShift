using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IWorkingTimeRuleService
{
    Task<List<WorkingTimeRuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkingTimeRuleResponse> CreateAsync(CreateWorkingTimeRuleRequest request, Guid createdBy, CancellationToken cancellationToken);
}
