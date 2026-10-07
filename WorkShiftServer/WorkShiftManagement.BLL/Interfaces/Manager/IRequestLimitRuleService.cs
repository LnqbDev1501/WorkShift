using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IRequestLimitRuleService
{
    Task<PagedResponse<RequestLimitRuleResponse>> GetPagedAsync(Guid managerId, RequestLimitRuleListQuery query, CancellationToken ct);
    Task<RequestLimitRuleResponse> GetAsync(Guid managerId, int id, CancellationToken ct);
    Task<RequestLimitRuleResponse> CreateAsync(Guid managerId, CreateRequestLimitRuleRequest request, CancellationToken ct);
    Task<RequestLimitRuleResponse> UpdateAsync(Guid managerId, int id, UpdateRequestLimitRuleRequest request, CancellationToken ct);
    Task<RequestLimitRuleResponse> PublishAsync(Guid managerId, int id, CancellationToken ct);
    Task<RequestLimitRuleResponse> ReplaceAsync(Guid managerId, int id, ReplaceRequestLimitRuleRequest request, CancellationToken ct);
}
