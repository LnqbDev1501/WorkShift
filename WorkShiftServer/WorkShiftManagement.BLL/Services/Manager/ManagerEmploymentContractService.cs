using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

using System.IO;

using WorkShiftManagement.BLL.Interfaces.Upload;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerEmploymentContractService(
    IEmploymentContractRepo repository,
    ICloudinaryService? cloudinaryService = null,
    IContractPdfGenerator? pdfGenerator = null) : IManagerEmploymentContractService
{
    private readonly IContractPdfGenerator _pdfGenerator = pdfGenerator ?? new ContractPdfGenerator();
    private readonly ICloudinaryService? _cloudinaryService = cloudinaryService;
    public async Task<CreateEmploymentContractResponse> CreateAsync(Guid managerId,
        CreateEmploymentContractRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (request.StaffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId is required.");
        if (request.StartDate == default || (request.EndDate != null && request.EndDate <= request.StartDate))
            throw new ArgumentException("StartDate is required and EndDate must be later than StartDate.");
        if (string.IsNullOrWhiteSpace(request.EmployeePosition) || request.EmployeePosition.Length > 100)
            throw new ArgumentException("EmployeePosition is required and must not exceed 100 characters.");

        var staff = await repository.GetCreationUserAsync(request.StaffUserId, cancellationToken);
        if (staff == null || staff.DeletedAt != null)
            throw new KeyNotFoundException("Staff does not exist.");
        if (!string.Equals(staff.Role?.RoleName, "STAFF", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The selected user must have the STAFF role.");
        if (staff.Status != "PENDING" && staff.Status != "ACTIVE")
            throw new ArgumentException("Staff must be PENDING or ACTIVE.");

        bool isRenewal = string.Equals(request.ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase) ||
                         (request.ReplacesContractId.HasValue && request.ReplacesContractId.Value > 0);

        int? replacesContractId = null;
        if (isRenewal)
        {
            if (staff.BranchId != manager.BranchId)
                throw new UnauthorizedAccessException("Staff does not belong to the Manager's branch.");

            if (request.ReplacesContractId.HasValue && request.ReplacesContractId.Value > 0)
            {
                var oldContract = await repository.GetByIdAsync(request.ReplacesContractId.Value, cancellationToken);
                if (oldContract == null || oldContract.UserId != staff.UserId || oldContract.BranchId != manager.BranchId!.Value)
                    throw new ArgumentException("Invalid old contract for renewal.");
                replacesContractId = oldContract.ContractId;
            }
            else
            {
                var oldContract = await repository.GetLatestContractForStaffInBranchAsync(staff.UserId, manager.BranchId!.Value, cancellationToken);
                if (oldContract == null)
                    throw new ArgumentException("Staff has no prior contract in this branch to renew.");
                replacesContractId = oldContract.ContractId;
            }
        }
        else
        {
            if (await repository.HasAnyContractAsync(staff.UserId, cancellationToken))
                throw new InvalidOperationException("Staff already has an employment contract. For existing staff, please use contract renewal instead.");
        }

        // Manager's branch takes precedence for branch, region, and minimum wage
        var branch = manager.Branch ?? staff.Branch;
        if (branch == null || branch.Status != "ACTIVE")
            throw new ArgumentException("Manager branch must be ACTIVE.");
        var region = branch.Region;
        var minimumHourlyRate = ValidateAndGetMinimumWage(region, request.StartDate);

        // Existing database index uq_one_pending_contract_per_staff also enforces this under concurrency.
        if (await repository.HasPendingContractAsync(staff.UserId, cancellationToken))
            throw new InvalidOperationException("Staff already has a draft or pending employment contract.");

        var now = DateTime.UtcNow;
        var contract = new EmploymentContract
        {
            UserId = staff.UserId,
            ContractNumber = $"EC-{Guid.NewGuid():N}",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            HourlyRate = minimumHourlyRate,
            EmployeePositionSnapshot = request.EmployeePosition.Trim(),
            BranchId = manager.BranchId!.Value,
            RegionId = region!.RegionId,
            EmployeeFullNameSnapshot = staff.FullName,
            EmployeePhoneSnapshot = staff.Phone,
            EmployeeAddressSnapshot = staff.Address,
            EmployeeIdentityNoSnapshot = staff.IdentityNo,
            BranchNameSnapshot = branch.BranchName,
            BranchAddressSnapshot = branch.Address,
            RegionNameSnapshot = region.RegionName,
            ContractTerms = request.ContractTerms,
            Status = "DRAFT",
            ReplacesContractId = replacesContractId,
            CreatedBy = managerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.CreateDraftAsync(contract, cancellationToken);
        return new(contract.ContractId, contract.ContractNumber, contract.UserId, contract.EmployeeFullNameSnapshot,
            contract.BranchId, contract.BranchNameSnapshot, contract.RegionId, contract.RegionNameSnapshot,
            contract.StartDate, contract.EndDate, contract.HourlyRate, contract.EmployeePositionSnapshot,
            contract.ContractTerms, contract.Status, contract.CreatedBy, contract.CreatedAt);
    }

    public async Task<IReadOnlyList<ContractStaffOptionResponse>> GetStaffOptionsAsync(Guid managerId, string? mode = null, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (string.Equals(mode, "RENEWAL", StringComparison.OrdinalIgnoreCase))
        {
            var renewalStaff = await repository.GetStaffForRenewalAsync(manager.BranchId!.Value, cancellationToken);
            return renewalStaff.Select(x => new ContractStaffOptionResponse(
                x.User.UserId,
                x.User.FullName,
                x.Contract.ContractId,
                x.Contract.ContractNumber,
                x.Contract.EmployeePositionSnapshot
            )).ToList();
        }

        var staff = await repository.GetStaffWithoutContractAsync(cancellationToken);
        return staff.Select(u => new ContractStaffOptionResponse(u.UserId, u.FullName)).ToList();
    }

    public async Task<PagedResponse<ManagerEmploymentContractListItemResponse>> GetPagedAsync(Guid managerId,
        ManagerEmploymentContractListRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (request.PageNumber < 1)
            throw new ArgumentException("PageNumber must be greater than 0.");
        if (request.PageSize < 1 || request.PageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.");

        var (items, totalCount) = await repository.GetManagerPagedAsync(
            manager.BranchId!.Value,
            request.StaffUserId,
            request.Status,
            request.ContractNumber,
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mapped = items.Select(MapToListItemResponse).ToList();
        return new PagedResponse<ManagerEmploymentContractListItemResponse>
        {
            Items = mapped,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = totalCount
        };
    }

    public async Task<ManagerEmploymentContractDetailResponse> GetByIdAsync(Guid managerId, int contractId,
        CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");

        return MapToDetailResponse(contract);
    }

    public async Task<ManagerEmploymentContractDetailResponse> UpdateDraftAsync(Guid managerId, int contractId,
        UpdateEmploymentContractRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");
        if (contract.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be edited.");

        if (request.StartDate == default || (request.EndDate != null && request.EndDate <= request.StartDate))
            throw new ArgumentException("StartDate is required and EndDate must be later than StartDate.");
        if (string.IsNullOrWhiteSpace(request.EmployeePosition) || request.EmployeePosition.Length > 100)
            throw new ArgumentException("EmployeePosition is required and must not exceed 100 characters.");

        var region = contract.Region;
        var minimumHourlyRate = ValidateAndGetMinimumWage(region, request.StartDate);

        var oldSnapshot = new
        {
            contract.StartDate,
            contract.EndDate,
            contract.HourlyRate,
            contract.EmployeePositionSnapshot,
            contract.ContractTerms,
            contract.UpdatedAt
        };

        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.HourlyRate = minimumHourlyRate;
        contract.EmployeePositionSnapshot = request.EmployeePosition.Trim();
        contract.ContractTerms = request.ContractTerms;

        await repository.UpdateDraftAsync(contract, managerId, oldSnapshot, cancellationToken);
        return MapToDetailResponse(contract);
    }

    public async Task<ManagerEmploymentContractDetailResponse> CancelDraftAsync(Guid managerId, int contractId,
        CancelEmploymentContractRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ArgumentException("Reason is required and must not exceed 500 characters.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");
        if (contract.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be cancelled.");

        var reason = request.Reason.Trim();
        await repository.CancelDraftAsync(contractId, managerId, reason, cancellationToken);

        contract.Status = "CANCELLED";
        contract.TerminationReason = reason;
        contract.UpdatedAt = DateTime.UtcNow;

        return MapToDetailResponse(contract);
    }

    public async Task<byte[]> GetPdfPreviewAsync(Guid managerId, int contractId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");

        return _pdfGenerator.GeneratePdf(contract, isPreview: true);
    }

    public async Task<ManagerEmploymentContractDetailResponse> SendContractAsync(Guid managerId, int contractId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");
        if (contract.Status != "DRAFT")
            throw new InvalidOperationException("Only draft contracts can be sent to staff.");

        var now = DateTime.UtcNow;
        var officialPdfBytes = _pdfGenerator.GeneratePdf(contract, isPreview: false);

        string publicId;
        if (_cloudinaryService != null)
        {
            publicId = await _cloudinaryService.UploadContractPdfAsync(contract.ContractNumber, officialPdfBytes);
        }
        else
        {
            var storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "storage", "contracts");
            Directory.CreateDirectory(storageDirectory);
            var fileName = $"{contract.ContractId}_{Guid.NewGuid():N}.pdf";
            publicId = Path.Combine(storageDirectory, fileName);
            await File.WriteAllBytesAsync(publicId, officialPdfBytes, cancellationToken);
        }

        const string templateVersion = "1.0";
        try
        {
            await repository.SendContractPdfAsync(contractId, publicId, templateVersion, now, now, managerId, cancellationToken);
        }
        catch (Exception)
        {
            if (_cloudinaryService != null)
            {
                try
                {
                    await _cloudinaryService.DeleteRawFileAsync(publicId);
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"[Cloudinary Cleanup Error] Failed to delete orphan raw file '{publicId}': {cleanupEx.Message}");
                }
            }
            throw;
        }

        contract.ContractFilePath = publicId;
        contract.ContractTemplateVersion = templateVersion;
        contract.PdfGeneratedAt = now;
        contract.SentToStaffAt = now;
        contract.Status = "PENDING_STAFF";
        contract.UpdatedAt = now;

        try
        {
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                NotificationType = "SYSTEM",
                Title = "Hợp đồng lao động mới",
                Content = $"Bạn đã nhận được Hợp đồng lao động mới (Mã HĐ: {contract.ContractNumber}). Vui lòng xem chi tiết và phản hồi.",
                CreatedBy = managerId,
                TargetBranchId = contract.BranchId,
                CreatedAt = now,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { contract.UserId }, cancellationToken);
        }
        catch
        {
            // Notification post-commit logging/fail-safe
        }

        return MapToDetailResponse(contract);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetPdfFileAsync(Guid managerId, int contractId, bool isDownload, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Contract does not belong to the Manager's branch.");

        if (string.IsNullOrWhiteSpace(contract.ContractFilePath))
            throw new KeyNotFoundException("The requested contract PDF file was not found.");

        byte[] bytes;
        bool isLocalPath = Path.IsPathRooted(contract.ContractFilePath) ||
                           contract.ContractFilePath.Contains("storage/contracts", StringComparison.OrdinalIgnoreCase) ||
                           contract.ContractFilePath.Contains("storage\\contracts", StringComparison.OrdinalIgnoreCase);

        if (isLocalPath)
        {
            if (!File.Exists(contract.ContractFilePath))
                throw new KeyNotFoundException("File PDF hợp đồng cũ trên máy chủ không tồn tại.");
            bytes = await File.ReadAllBytesAsync(contract.ContractFilePath, cancellationToken);
        }
        else
        {
            if (_cloudinaryService == null)
                throw new InvalidOperationException("Cloudinary service is not configured.");
            bytes = await _cloudinaryService.DownloadRawFileAsync(contract.ContractFilePath);
        }

        var fileName = $"{contract.ContractNumber}.pdf";
        return (bytes, "application/pdf", fileName);
    }

    private static decimal ValidateAndGetMinimumWage(Region? region, DateOnly startDate)
    {
        if (region == null || !region.IsActive)
            throw new ArgumentException("Branch region must be active.");
        var wage = region.RegionalMinimumWage;
        if (region.RegionalMinimumWageId == null || wage == null || wage.RegionalMinimumWageId != region.RegionalMinimumWageId)
            throw new ArgumentException("Region has no assigned minimum wage configuration.");
        // Activation closes the old wage on the day before its replacement: both bounds are inclusive.
        if (wage.Status != "ACTIVE" || wage.MinimumHourlyRate <= 0 || wage.EffectiveFrom == null ||
            startDate < wage.EffectiveFrom || (wage.EffectiveTo != null && startDate > wage.EffectiveTo))
            throw new ArgumentException("Regional minimum wage must be ACTIVE and effective on the contract start date.");

        return wage.MinimumHourlyRate;
    }

    private async Task<User> RequireManagerAsync(Guid managerId, CancellationToken cancellationToken)
    {
        var manager = await repository.GetCreationUserAsync(managerId, cancellationToken);
        if (manager == null || manager.DeletedAt != null || manager.Status != "ACTIVE" ||
            !string.Equals(manager.Role?.RoleName, "MANAGER", StringComparison.OrdinalIgnoreCase) ||
            manager.Role?.Status != "ACTIVE" || manager.BranchId == null)
            throw new UnauthorizedAccessException("An active Manager with an assigned branch is required.");

        return manager;
    }

    private static ManagerEmploymentContractListItemResponse MapToListItemResponse(EmploymentContract c)
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

        return new ManagerEmploymentContractListItemResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            StaffUserId = c.UserId,
            EmployeeFullName = staffName,
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
            UpdatedAt = c.UpdatedAt,
            HasPdf = !string.IsNullOrWhiteSpace(c.ContractFilePath)
        };
    }

    private static ManagerEmploymentContractDetailResponse MapToDetailResponse(EmploymentContract c)
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

        return new ManagerEmploymentContractDetailResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            StaffUserId = c.UserId,
            EmployeeFullName = fullName,
            EmployeePhone = phone,
            EmployeeAddress = address,
            EmployeeIdentityNo = identityNo,
            EmployeePosition = position,
            BranchId = c.BranchId,
            BranchName = branchName,
            BranchAddress = branchAddress,
            RegionId = c.RegionId,
            RegionName = regionName,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            ContractTerms = c.ContractTerms,
            Status = c.Status,
            ReplacesContractId = c.ReplacesContractId,
            TerminationReason = c.TerminationReason,
            CreatedBy = c.CreatedBy,
            CreatedByManagerName = !string.IsNullOrWhiteSpace(c.CreatedByNavigation?.FullName) ? c.CreatedByNavigation.FullName : null,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            HasPdf = !string.IsNullOrWhiteSpace(c.ContractFilePath)
        };
    }
}
