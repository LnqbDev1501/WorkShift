using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IEmployeeAdjustmentService
{
    Task<PagedResponse<EmployeeAdjustmentResponse>> ListAsync(Guid actor, string type, EmployeeAdjustmentQuery query, CancellationToken ct);
    Task<EmployeeAdjustmentResponse> DetailAsync(Guid actor, string type, int id, CancellationToken ct);
    Task<EmployeeAdjustmentResponse> CreateAsync(Guid actor, string type, SaveEmployeeAdjustmentRequest request, CancellationToken ct);
    Task<EmployeeAdjustmentResponse> UpdateAsync(Guid actor, string type, int id, SaveEmployeeAdjustmentRequest request, CancellationToken ct);
    Task DeleteAsync(Guid actor, string type, int id, CancellationToken ct);
    Task<EmployeeAdjustmentResponse> ReviewAsync(Guid actor, string type, int id, bool approve, ReviewEmployeeAdjustmentRequest request, CancellationToken ct);
}
