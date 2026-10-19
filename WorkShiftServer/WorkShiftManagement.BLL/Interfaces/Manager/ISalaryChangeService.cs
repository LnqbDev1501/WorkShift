using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.SalaryChanges;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface ISalaryChangeService
{
    Task<PagedResponse<SalaryChangeResponse>> ListAsync(Guid actor, SalaryChangeQuery query, CancellationToken ct);
    Task<SalaryChangeResponse> DetailAsync(Guid actor, int id, CancellationToken ct);
    Task<SalaryChangeResponse> CreateAsync(Guid actor, CreateSalaryChangeRequest request, CancellationToken ct);
    Task<SalaryChangeResponse> CancelAsync(Guid actor, int id, CancelSalaryChangeRequest request, CancellationToken ct);
    Task<SalaryChangeResponse> ApproveAsync(Guid actor, int id, ApproveSalaryChangeRequest request, CancellationToken ct);
    Task<SalaryChangeResponse> RejectAsync(Guid actor, int id, RejectSalaryChangeRequest request, CancellationToken ct);
    Task<int> ActivateDueAsync(CancellationToken ct);
}
