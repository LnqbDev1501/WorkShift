using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerEmploymentContractService
{
    Task<IReadOnlyList<ContractStaffOptionResponse>> GetStaffOptionsAsync(Guid managerId, string? mode = null, CancellationToken cancellationToken = default);
    Task<CreateEmploymentContractResponse> CreateAsync(Guid managerId, CreateEmploymentContractRequest request,
        CancellationToken cancellationToken = default);
    Task<PagedResponse<ManagerEmploymentContractListItemResponse>> GetPagedAsync(Guid managerId,
        ManagerEmploymentContractListRequest request, CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> GetByIdAsync(Guid managerId, int contractId,
        CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> UpdateDraftAsync(Guid managerId, int contractId,
        UpdateEmploymentContractRequest request, CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> CancelDraftAsync(Guid managerId, int contractId,
        CancelEmploymentContractRequest request, CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> CancelContractAsync(Guid managerId, int contractId,
        CancelEmploymentContractRequest request, CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> TerminateContractAsync(Guid managerId, int contractId,
        TerminateEmploymentContractRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GetPdfPreviewAsync(Guid managerId, int contractId, CancellationToken cancellationToken = default);
    Task<ManagerEmploymentContractDetailResponse> SendContractAsync(Guid managerId, int contractId, CancellationToken cancellationToken = default);
    Task<(byte[] Bytes, string ContentType, string FileName)> GetPdfFileAsync(Guid managerId, int contractId, bool isDownload, CancellationToken cancellationToken = default);

    // Contract Change Requests
    Task<PagedResponse<ManagerContractChangeRequestResponse>> GetChangeRequestsAsync(Guid managerId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ManagerContractChangeRequestResponse> GetChangeRequestByIdAsync(Guid managerId, int changeRequestId, CancellationToken cancellationToken = default);
    Task<ManagerContractChangeRequestResponse> ApproveChangeRequestAsync(Guid managerId, int changeRequestId, ApproveContractChangeRequest request, CancellationToken cancellationToken = default);
    Task<ManagerContractChangeRequestResponse> RejectChangeRequestAsync(Guid managerId, int changeRequestId, RejectContractChangeRequest request, CancellationToken cancellationToken = default);

    // History
    Task<ManagerContractHistoryResponse> GetContractHistoryAsync(Guid managerId, int contractId, int pageNumber = 1, int pageSize = 20, string? sortOrder = "desc", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffContractHistoryItemResponse>> GetStaffContractHistoryAsync(Guid managerId, Guid staffUserId, CancellationToken cancellationToken = default);
}
