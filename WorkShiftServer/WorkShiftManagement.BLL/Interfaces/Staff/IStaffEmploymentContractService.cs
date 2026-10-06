using System;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using WorkShiftManagement.BLL.DTOs.Staff.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IStaffEmploymentContractService
{
    Task<(byte[] Bytes, string ContentType, string FileName)> GetStaffPdfFileAsync(Guid staffUserId, int contractId, bool isDownload, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffContractListItemResponse>> GetMyContractsAsync(Guid staffUserId, CancellationToken cancellationToken = default);
    Task<StaffContractDetailResponse> GetMyContractByIdAsync(Guid staffUserId, int contractId, CancellationToken cancellationToken = default);
    Task<StaffContractDetailResponse> AcceptContractAsync(Guid staffUserId, int contractId, CancellationToken cancellationToken = default);
    Task<bool> CanStaffRegisterShiftAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default);
    Task ValidateStaffContractForShiftRegistrationAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default);
    Task<int> ActivateDueScheduledContractsAsync(CancellationToken cancellationToken = default);
}
