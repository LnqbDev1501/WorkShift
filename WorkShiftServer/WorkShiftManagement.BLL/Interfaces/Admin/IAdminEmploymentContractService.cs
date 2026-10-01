using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IAdminEmploymentContractService
{
    Task<PagedResponse<AdminEmploymentContractListItemResponse>> GetPagedAsync(
        AdminEmploymentContractListRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminEmploymentContractDetailResponse> GetByIdAsync(
        int contractId,
        CancellationToken cancellationToken = default);
}
