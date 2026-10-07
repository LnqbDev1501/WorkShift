using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Staff;

public class StaffEmploymentContractService(
    IEmploymentContractRepo repository,
    ICloudinaryService? cloudinaryService = null) : IStaffEmploymentContractService
{
    private readonly ICloudinaryService? _cloudinaryService = cloudinaryService;

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetStaffPdfFileAsync(Guid staffUserId, int contractId, bool isDownload, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found.");

        if (contract.UserId != staffUserId)
            throw new UnauthorizedAccessException("You can only access your own employment contracts.");

        if (string.Equals(contract.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Draft contracts cannot be accessed by staff until sent.");

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

    public async Task<IReadOnlyList<StaffContractListItemResponse>> GetMyContractsAsync(Guid staffUserId, CancellationToken cancellationToken = default)
    {
        var contracts = await repository.GetStaffContractsAsync(staffUserId, cancellationToken);
        return contracts.Select(c => new StaffContractListItemResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            EmployeePosition = !string.IsNullOrWhiteSpace(c.EmployeePositionSnapshot) ? c.EmployeePositionSnapshot : string.Empty,
            BranchId = c.BranchId,
            BranchName = !string.IsNullOrWhiteSpace(c.BranchNameSnapshot) ? c.BranchNameSnapshot : (c.Branch?.BranchName ?? string.Empty),
            RegionId = c.RegionId,
            RegionName = !string.IsNullOrWhiteSpace(c.RegionNameSnapshot) ? c.RegionNameSnapshot : (c.Region?.RegionName ?? string.Empty),
            CreatedAt = c.CreatedAt,
            SentToStaffAt = c.SentToStaffAt,
            StaffAcceptedAt = c.StaffAcceptedAt,
            HasPdf = !string.IsNullOrWhiteSpace(c.ContractFilePath)
        }).ToList();
    }

    public async Task<StaffContractDetailResponse> GetMyContractByIdAsync(Guid staffUserId, int contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetStaffContractByIdAsync(contractId, staffUserId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found or not sent to staff.");

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        bool isPending = string.Equals(contract.Status, "PENDING_STAFF", StringComparison.OrdinalIgnoreCase);
        bool isPastStart = todayVn > contract.StartDate;

        bool canAccept = isPending && !isPastStart;
        string? cannotAcceptReason = null;
        if (isPending && isPastStart)
        {
            cannotAcceptReason = "Hợp đồng đã quá ngày bắt đầu. Vui lòng yêu cầu Manager điều chỉnh ngày bắt đầu và gửi lại.";
        }

        return new StaffContractDetailResponse
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            HourlyRate = contract.HourlyRate,
            EmployeePosition = !string.IsNullOrWhiteSpace(contract.EmployeePositionSnapshot) ? contract.EmployeePositionSnapshot : string.Empty,
            ContractTerms = contract.ContractTerms,
            BranchId = contract.BranchId,
            BranchName = !string.IsNullOrWhiteSpace(contract.BranchNameSnapshot) ? contract.BranchNameSnapshot : (contract.Branch?.BranchName ?? string.Empty),
            BranchAddress = !string.IsNullOrWhiteSpace(contract.BranchAddressSnapshot) ? contract.BranchAddressSnapshot : (contract.Branch?.Address ?? string.Empty),
            RegionId = contract.RegionId,
            RegionName = !string.IsNullOrWhiteSpace(contract.RegionNameSnapshot) ? contract.RegionNameSnapshot : (contract.Region?.RegionName ?? string.Empty),
            CreatedByManagerName = contract.CreatedByNavigation?.FullName,
            CreatedAt = contract.CreatedAt,
            SentToStaffAt = contract.SentToStaffAt,
            StaffAcceptedAt = contract.StaffAcceptedAt,
            HasPdf = !string.IsNullOrWhiteSpace(contract.ContractFilePath),
            CanAccept = canAccept,
            CannotAcceptReason = cannotAcceptReason
        };
    }

    public async Task<StaffContractDetailResponse> AcceptContractAsync(Guid staffUserId, int contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetStaffContractByIdAsync(contractId, staffUserId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract was not found or not sent to staff.");

        if (!string.Equals(contract.Status, "PENDING_STAFF", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only contracts in PENDING_STAFF status can be accepted.");

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (todayVn > contract.StartDate)
        {
            throw new InvalidOperationException("Hợp đồng đã quá ngày bắt đầu. Vui lòng yêu cầu Manager điều chỉnh ngày bắt đầu và gửi lại.");
        }

        var newStatus = (todayVn == contract.StartDate) ? "ACTIVE" : "SCHEDULED";
        var nowUtc = DateTime.UtcNow;

        await repository.StaffAcceptContractAsync(contractId, staffUserId, newStatus, nowUtc, cancellationToken);

        // Notify Manager post-commit
        try
        {
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                NotificationType = "SYSTEM",
                Title = "Nhân viên đã chấp nhận hợp đồng",
                Content = $"Nhân viên {contract.EmployeeFullNameSnapshot} đã chấp nhận hợp đồng lao động {contract.ContractNumber}. Trạng thái hợp đồng: {newStatus}.",
                CreatedBy = staffUserId,
                TargetBranchId = contract.BranchId,
                CreatedAt = nowUtc,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { contract.CreatedBy }, cancellationToken);
        }
        catch
        {
            // Logging fail-safe
        }

        return await GetMyContractByIdAsync(staffUserId, contractId, cancellationToken);
    }

    public async Task<bool> CanStaffRegisterShiftAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default)
    {
        if (staffUserId == Guid.Empty || branchId <= 0)
            return false;
        if (scheduledEndAtUtc <= scheduledStartAtUtc)
            return false;

        return await repository.HasActiveContractForShiftRegistrationAsync(staffUserId, branchId, scheduledStartAtUtc, scheduledEndAtUtc, cancellationToken);
    }

    public async Task ValidateStaffContractForShiftRegistrationAsync(Guid staffUserId, int branchId, DateTime scheduledStartAtUtc, DateTime scheduledEndAtUtc, CancellationToken cancellationToken = default)
    {
        if (staffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId is required.");
        if (branchId <= 0)
            throw new ArgumentException("BranchId must be greater than 0.");
        if (scheduledEndAtUtc <= scheduledStartAtUtc)
            throw new ArgumentException("ScheduledEndAt must be after ScheduledStartAt.");

        var hasActive = await repository.HasActiveContractForShiftRegistrationAsync(staffUserId, branchId, scheduledStartAtUtc, scheduledEndAtUtc, cancellationToken);
        if (!hasActive)
        {
            throw new InvalidOperationException("Bạn không thể đăng ký ca làm việc này. Bạn phải có hợp đồng lao động đang ở trạng thái ACTIVE, thuộc chi nhánh của ca và nằm trong thời hạn hiệu lực của hợp đồng (kể cả giờ bắt đầu và kết thúc ca). Hợp đồng SCHEDULED hoặc PENDING chưa cho phép đăng ký ca.");
        }
    }

    public async Task<int> ActivateDueScheduledContractsAsync(CancellationToken cancellationToken = default)
    {
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        return await repository.ActivateDueScheduledContractsAsync(todayVn, cancellationToken);
    }
}
