using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerEmploymentContractService
{
    Task<IReadOnlyList<ContractStaffOptionResponse>> GetStaffOptionsAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<CreateEmploymentContractResponse> CreateAsync(Guid managerId, CreateEmploymentContractRequest request,
        CancellationToken cancellationToken = default);
}
