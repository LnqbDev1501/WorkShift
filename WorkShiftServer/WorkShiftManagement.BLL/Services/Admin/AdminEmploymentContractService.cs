using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class AdminEmploymentContractService : IAdminEmploymentContractService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "DRAFT",
        "PENDING_STAFF",
        "PENDING_MANAGER_APPROVAL",
        "ACTIVE",
        "REJECTED",
        "TERMINATED",
        "EXPIRED",
        "SUPERSEDED",
        "CANCELLED"
    };

    private readonly IEmploymentContractRepo _contractRepo;

    public AdminEmploymentContractService(IEmploymentContractRepo contractRepo)
    {
        _contractRepo = contractRepo;
    }

    public async Task<PagedResponse<AdminEmploymentContractListItemResponse>> GetPagedAsync(
        AdminEmploymentContractListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        if (request.PageNumber < 1)
        {
            throw new ArgumentException("PageNumber must be greater than 0.");
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        long offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue)
        {
            throw new ArgumentException("PageNumber and PageSize result in an offset that exceeds the maximum supported limit.");
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var trimmedStatus = request.Status.Trim().ToUpperInvariant();
            if (!ValidStatuses.Contains(trimmedStatus))
            {
                throw new ArgumentException($"Invalid contract status '{request.Status}'. Valid statuses are: DRAFT, PENDING_STAFF, PENDING_MANAGER_APPROVAL, ACTIVE, REJECTED, TERMINATED, EXPIRED, SUPERSEDED, CANCELLED.");
            }
            normalizedStatus = trimmedStatus;
        }

        if (request.BranchId.HasValue && request.BranchId.Value <= 0)
        {
            throw new ArgumentException("BranchId must be greater than 0.");
        }

        if (request.RegionId.HasValue && request.RegionId.Value <= 0)
        {
            throw new ArgumentException("RegionId must be greater than 0.");
        }

        if (request.StartDateFrom.HasValue && request.StartDateTo.HasValue && request.StartDateFrom.Value > request.StartDateTo.Value)
        {
            throw new ArgumentException("StartDateFrom cannot be greater than StartDateTo.");
        }

        if (request.EndDateFrom.HasValue && request.EndDateTo.HasValue && request.EndDateFrom.Value > request.EndDateTo.Value)
        {
            throw new ArgumentException("EndDateFrom cannot be greater than EndDateTo.");
        }

        var (items, totalCount) = await _contractRepo.GetPagedAsync(
            request.Search,
            normalizedStatus,
            request.BranchId,
            request.RegionId,
            request.StartDateFrom,
            request.StartDateTo,
            request.EndDateFrom,
            request.EndDateTo,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mappedItems = items.Select(MapToListItemResponse).ToList();

        return new PagedResponse<AdminEmploymentContractListItemResponse>
        {
            Items = mappedItems,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = totalCount
        };
    }

    public async Task<AdminEmploymentContractDetailResponse> GetByIdAsync(
        int contractId,
        CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            throw new ArgumentException("ContractId must be greater than 0.");
        }

        var contract = await _contractRepo.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
        {
            throw new KeyNotFoundException($"Employment contract with ID {contractId} was not found.");
        }

        return MapToDetailResponse(contract);
    }

    private static AdminEmploymentContractListItemResponse MapToListItemResponse(EmploymentContract c)
    {
        var staffName = !string.IsNullOrWhiteSpace(c.EmployeeFullNameSnapshot)
            ? c.EmployeeFullNameSnapshot
            : (c.User?.FullName ?? string.Empty);

        var position = !string.IsNullOrWhiteSpace(c.EmployeePositionSnapshot)
            ? c.EmployeePositionSnapshot
            : (c.User?.EmployeeType?.TypeName ?? string.Empty);

        var branchName = !string.IsNullOrWhiteSpace(c.BranchNameSnapshot)
            ? c.BranchNameSnapshot
            : (c.Branch?.BranchName ?? string.Empty);

        var regionName = !string.IsNullOrWhiteSpace(c.RegionNameSnapshot)
            ? c.RegionNameSnapshot
            : (c.Region?.RegionName ?? string.Empty);

        return new AdminEmploymentContractListItemResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            StaffId = c.UserId,
            StaffName = staffName,
            EmployeePosition = position,
            BranchId = c.BranchId,
            BranchName = branchName,
            RegionId = c.RegionId,
            RegionName = regionName,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            HasPdf = !string.IsNullOrWhiteSpace(c.ContractFilePath)
        };
    }

    private static AdminEmploymentContractDetailResponse MapToDetailResponse(EmploymentContract c)
    {
        var fullName = !string.IsNullOrWhiteSpace(c.EmployeeFullNameSnapshot)
            ? c.EmployeeFullNameSnapshot
            : (c.User?.FullName ?? string.Empty);

        var phone = !string.IsNullOrWhiteSpace(c.EmployeePhoneSnapshot)
            ? c.EmployeePhoneSnapshot
            : (c.User?.Phone ?? string.Empty);

        var address = !string.IsNullOrWhiteSpace(c.EmployeeAddressSnapshot)
            ? c.EmployeeAddressSnapshot
            : (c.User?.Address ?? string.Empty);

        var identityNo = !string.IsNullOrWhiteSpace(c.EmployeeIdentityNoSnapshot)
            ? c.EmployeeIdentityNoSnapshot
            : (c.User?.IdentityNo ?? string.Empty);

        var position = !string.IsNullOrWhiteSpace(c.EmployeePositionSnapshot)
            ? c.EmployeePositionSnapshot
            : (c.User?.EmployeeType?.TypeName ?? string.Empty);

        var branchName = !string.IsNullOrWhiteSpace(c.BranchNameSnapshot)
            ? c.BranchNameSnapshot
            : (c.Branch?.BranchName ?? string.Empty);

        var branchAddress = !string.IsNullOrWhiteSpace(c.BranchAddressSnapshot)
            ? c.BranchAddressSnapshot
            : (c.Branch?.Address ?? string.Empty);

        var regionName = !string.IsNullOrWhiteSpace(c.RegionNameSnapshot)
            ? c.RegionNameSnapshot
            : (c.Region?.RegionName ?? string.Empty);

        return new AdminEmploymentContractDetailResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            ContractTerms = c.ContractTerms,
            ReplacesContractId = c.ReplacesContractId,

            StaffId = c.UserId,
            FullName = fullName,
            Phone = phone,
            Address = address,
            IdentityNo = identityNo,
            EmployeePosition = position,

            BranchId = c.BranchId,
            BranchName = branchName,
            BranchAddress = branchAddress,
            RegionId = c.RegionId,
            RegionName = regionName,

            CreatedBy = c.CreatedBy,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            SentToStaffAt = c.SentToStaffAt,
            StaffRespondedAt = c.StaffRespondedAt,
            StaffAcceptedAt = c.StaffAcceptedAt,
            ApprovedBy = c.ApprovedBy,
            ApprovedAt = c.ApprovedAt,
            RejectedBy = c.RejectedBy,
            RejectedAt = c.RejectedAt,
            ManagerRejectionComment = c.ManagerRejectionComment,
            TerminatedBy = c.TerminatedBy,
            TerminatedAt = c.TerminatedAt,
            TerminationEffectiveAt = c.TerminationEffectiveAt,
            TerminationReason = c.TerminationReason,

            HasPdf = !string.IsNullOrWhiteSpace(c.ContractFilePath),
            ContractTemplateVersion = c.ContractTemplateVersion,
            PdfGeneratedAt = c.PdfGeneratedAt
        };
    }
}
