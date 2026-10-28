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

    Task<AdminContractHistoryResponse> GetContractHistoryAsync(
        int contractId,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortOrder = "desc",
        CancellationToken cancellationToken = default);
}
