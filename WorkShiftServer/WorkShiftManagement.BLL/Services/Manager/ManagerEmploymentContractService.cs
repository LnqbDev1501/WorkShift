using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Helpers;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using NotificationEntity = WorkShiftManagement.DAL.Entities.Notification;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerEmploymentContractService(
    IEmploymentContractRepo repository,
    ICloudinaryService? cloudinaryService = null,
    IContractPdfGenerator? pdfGenerator = null) : IManagerEmploymentContractService
{
    private readonly IContractPdfGenerator _pdfGenerator = pdfGenerator ?? new ContractPdfGenerator();
    private readonly ICloudinaryService? _cloudinaryService = cloudinaryService;

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try 
        { 
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); 
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { 
            // Fallback for Windows platform
        }

        try 
        { 
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); 
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { 
            // Fallback to custom offset
        }

        return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "Indochina Time", "Indochina Time");
    }

    private static DateOnly GetTodayVn()
    {
        var vnNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone);
        return DateOnly.FromDateTime(vnNow.DateTime);
    }

    public async Task<CreateEmploymentContractResponse> CreateAsync(Guid managerId,
        CreateEmploymentContractRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (request.StaffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId is required.");
        if (request.StartDate == default || (request.EndDate != null && request.EndDate <= request.StartDate))
            throw new ArgumentException("StartDate is required and EndDate must be later than StartDate.");

        var todayVn = GetTodayVn();
        if (request.StartDate < todayVn)
            throw new ArgumentException("The contract start date cannot be in the past. It must be today or a future date.");

        var staff = await repository.GetCreationUserAsync(request.StaffUserId, cancellationToken);
        if (staff == null || staff.DeletedAt != null)
            throw new KeyNotFoundException("Staff does not exist.");

        var position = (string.IsNullOrWhiteSpace(request.EmployeePosition)
            ? staff.EmployeeType?.TypeName
            : request.EmployeePosition)?.Trim();

        if (string.IsNullOrWhiteSpace(position) || position.Length > 100)
            throw new ArgumentException("EmployeePosition is required and must not exceed 100 characters.");

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

            EmploymentContract? oldContract;
            if (request.ReplacesContractId.HasValue && request.ReplacesContractId.Value > 0)
            {
                oldContract = await repository.GetByIdAsync(request.ReplacesContractId.Value, cancellationToken);
                if (oldContract == null || oldContract.UserId != staff.UserId || oldContract.BranchId != manager.BranchId!.Value || oldContract.Status == "CANCELLED")
                    throw new ArgumentException("Cannot renew a cancelled contract. For this staff, please create a new contract instead.");
            }
            else
            {
                oldContract = await repository.GetLatestContractForStaffInBranchAsync(staff.UserId, manager.BranchId!.Value, cancellationToken);
                if (oldContract == null || oldContract.Status == "CANCELLED")
                    throw new ArgumentException("Staff has no prior active or expired contract in this branch to renew. Please use 'New Staff' contract creation instead.");
            }

            replacesContractId = oldContract.ContractId;

            if (request.StartDate < oldContract.StartDate)
            {
                throw new ArgumentException($"The new contract start date ({request.StartDate:yyyy-MM-dd}) cannot be earlier than the current contract start date ({oldContract.StartDate:yyyy-MM-dd}).");
            }
        }
        else
        {
            if (await repository.HasActiveOrScheduledContractAsync(staff.UserId, cancellationToken))
                throw new InvalidOperationException("Staff already has an active or scheduled employment contract. For existing active staff, please use contract renewal instead.");
        }

        var branch = manager.Branch ?? staff.Branch;
        if (branch == null || branch.Status != "ACTIVE")
            throw new ArgumentException("Manager branch must be ACTIVE.");
        var region = branch.Region;
        var minimumHourlyRate = ValidateAndGetMinimumWage(region, request.StartDate);

        decimal finalHourlyRate = minimumHourlyRate;
        if (request.HourlyRate.HasValue && request.HourlyRate.Value > 0)
        {
            if (request.HourlyRate.Value < minimumHourlyRate)
            {
                throw new ArgumentException($"Hourly rate ({request.HourlyRate.Value:N0} VNĐ/h) cannot be lower than the regional minimum wage ({minimumHourlyRate:N0} VNĐ/h).");
            }
            finalHourlyRate = request.HourlyRate.Value;
        }

        if (await repository.HasPendingContractAsync(staff.UserId, cancellationToken))
            throw new InvalidOperationException("Staff already has a draft or pending employment contract.");

        var now = DateTime.UtcNow;
        var contract = new EmploymentContract
        {
            UserId = staff.UserId,
            ContractNumber = $"EC-{Guid.NewGuid():N}",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            HourlyRate = finalHourlyRate,
            EmployeePositionSnapshot = position,
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
            var todayVn = GetTodayVn();

            return renewalStaff
                .Select(x =>
                {
                    var contract = x.Contract;
                    int? daysLeft = null;
                    string category;

                    if (!contract.EndDate.HasValue)
                    {
                        category = "INDEFINITE";
                    }
                    else
                    {
                        daysLeft = contract.EndDate.Value.DayNumber - todayVn.DayNumber;
                        if (daysLeft < 0)
                        {
                            category = "EXPIRED";
                        }
                        else if (daysLeft <= 7)
                        {
                            category = "URGENT"; // Sắp hết hạn gấp (<= 7 ngày) - Ưu tiên tạo
                        }
                        else if (daysLeft <= 30)
                        {
                            category = "UPCOMING"; // Chuẩn bị gia hạn (8 - 30 ngày)
                        }
                        else
                        {
                            category = "LONG_TERM"; // Còn hạn dài (> 30 ngày) - Thay thế / Thăng chức
                        }
                    }

                    return new ContractStaffOptionResponse(
                        x.User.UserId,
                        x.User.FullName,
                        contract.ContractId,
                        contract.ContractNumber,
                        contract.EmployeePositionSnapshot ?? x.User.EmployeeType?.TypeName,
                        contract.StartDate,
                        contract.EndDate,
                        x.LatestSalary ?? contract.HourlyRate,
                        contract.Status,
                        daysLeft,
                        category,
                        contract.ContractTerms
                    );
                })
                .OrderBy(x => x.ExpirationCategory switch
                {
                    "EXPIRED" => 1,
                    "URGENT" => 2,
                    "UPCOMING" => 3,
                    "LONG_TERM" => 4,
                    _ => 5 // INDEFINITE
                })
                .ThenBy(x => x.DaysUntilExpiration ?? int.MaxValue)
                .ThenBy(x => x.FullName)
                .ToList();
        }

        var regionalMinWage = manager.Branch?.Region?.RegionalMinimumWage?.MinimumHourlyRate;
        var staff = await repository.GetStaffWithoutContractAsync(manager.BranchId!.Value, cancellationToken);
        return staff.Select(u => new ContractStaffOptionResponse(
            u.UserId,
            u.FullName,
            CurrentPosition: u.EmployeeType?.TypeName,
            CurrentHourlyRate: regionalMinWage
        )).ToList();
    }

    public async Task<decimal> GetBranchMinimumWageAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        var wage = manager.Branch?.Region?.RegionalMinimumWage;
        return wage != null && wage.Status == "ACTIVE" && wage.MinimumHourlyRate > 0
            ? wage.MinimumHourlyRate
            : 0m;
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

    public Task<ManagerEmploymentContractDetailResponse> CancelDraftAsync(Guid managerId, int contractId,
        CancelEmploymentContractRequest request, CancellationToken cancellationToken = default)
        => CancelContractAsync(managerId, contractId, request, cancellationToken);

    public async Task<ManagerEmploymentContractDetailResponse> CancelContractAsync(Guid managerId, int contractId,
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

        var reason = request.Reason.Trim();
        var wasSent = !string.Equals(contract.Status, "DRAFT", StringComparison.OrdinalIgnoreCase);

        await repository.CancelContractAsync(contractId, managerId, reason, cancellationToken);

        contract.Status = "CANCELLED";
        contract.TerminationReason = reason;
        contract.TerminatedBy = managerId;
        contract.TerminatedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        if (wasSent)
        {
            try
            {
                var notification = new NotificationEntity
                {
                    NotificationType = "SYSTEM",
                    Title = "Hợp đồng lao động đã bị hủy",
                    Content = $"Hợp đồng lao động {contract.ContractNumber} đã bị quản lý hủy trước ngày bắt đầu. Lý do: {reason}.",
                    CreatedBy = managerId,
                    TargetBranchId = contract.BranchId,
                    TargetType = "Branch",
                    Channels = new() { "InApp" },
                    TargetRoles = new(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await repository.AddNotificationAsync(notification, new[] { contract.UserId }, cancellationToken);
            }
            catch
            {
                // Fail-safe notification logging
            }
        }

        return MapToDetailResponse(contract);
    }

    public async Task<ManagerEmploymentContractDetailResponse> TerminateContractAsync(Guid managerId, int contractId,
        TerminateEmploymentContractRequest request, CancellationToken cancellationToken = default)
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

        if (!string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể chấm dứt hợp đồng đang có hiệu lực (ACTIVE). Đối với hợp đồng chưa có hiệu lực, vui lòng sử dụng chức năng Hủy hợp đồng.");

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var effectiveDate = request.TerminationEffectiveDate ?? todayVn;
        if (effectiveDate < contract.StartDate)
            throw new ArgumentException("Ngày có hiệu lực chấm dứt hợp đồng không thể trước ngày bắt đầu hợp đồng.");

        var nowUtc = DateTime.UtcNow;
        var effectiveDateTimeUtc = effectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var reason = request.Reason.Trim();

        await repository.TerminateContractAsync(contractId, managerId, reason, effectiveDateTimeUtc, cancellationToken);

        contract.Status = "TERMINATED";
        contract.TerminationReason = reason;
        contract.TerminatedBy = managerId;
        contract.TerminatedAt = nowUtc;
        contract.TerminationEffectiveAt = effectiveDateTimeUtc;
        contract.UpdatedAt = nowUtc;

        try
        {
            var notification = new NotificationEntity
            {
                NotificationType = "SYSTEM",
                Title = "Hợp đồng lao động bị chấm dứt",
                Content = $"Hợp đồng lao động {contract.ContractNumber} đã được quản lý chấm dứt có hiệu lực từ ngày {effectiveDate:yyyy-MM-dd}. Lý do: {reason}.",
                CreatedBy = managerId,
                TargetBranchId = contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { contract.UserId }, cancellationToken);
        }
        catch
        {
            // Fail-safe notification logging
        }

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

        if (_cloudinaryService == null)
        {
            throw new InvalidOperationException("Dịch vụ Cloudinary chưa được cấu hình. Gửi hợp đồng yêu cầu tạo PDF và lưu trữ trên Cloudinary thành công.");
        }

        var now = DateTime.UtcNow;
        var officialPdfBytes = _pdfGenerator.GeneratePdf(contract, isPreview: false);

        string publicId;
        try
        {
            publicId = await _cloudinaryService.UploadContractPdfAsync(contract.ContractNumber, officialPdfBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Tải lên file PDF hợp đồng lên Cloudinary thất bại: {ex.Message}", ex);
        }

        var templateVersion = string.IsNullOrWhiteSpace(contract.ContractTemplateVersion) ? "1.0" : IncrementVersion(contract.ContractTemplateVersion);

        try
        {
            await repository.SendContractPdfAsync(contractId, publicId, templateVersion, now, now, managerId, cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                await _cloudinaryService.DeleteRawFileAsync(publicId);
            }
            catch (Exception cleanupEx)
            {
                Console.WriteLine($"[Cloudinary Cleanup Error] Failed to delete orphan raw file '{publicId}': {cleanupEx.Message}");
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
            var notification = new NotificationEntity
            {
                NotificationType = "SYSTEM",
                Title = "Hợp đồng lao động mới",
                Content = $"Bạn đã nhận được Hợp đồng lao động mới (Mã HĐ: {contract.ContractNumber}). Vui lòng xem chi tiết và phản hồi.",
                CreatedBy = managerId,
                TargetBranchId = contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { contract.UserId }, cancellationToken);
        }
        catch
        {
            // Fail-safe notification logging
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

        if (_cloudinaryService == null)
            throw new InvalidOperationException("Cloudinary service is not configured.");

        var bytes = await _cloudinaryService.DownloadRawFileAsync(contract.ContractFilePath);
        var fileName = $"{contract.ContractNumber}.pdf";
        return (bytes, "application/pdf", fileName);
    }

    public async Task<PagedResponse<ManagerContractChangeRequestResponse>> GetChangeRequestsAsync(
        Guid managerId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (pageNumber < 1)
            throw new ArgumentException("PageNumber must be greater than 0.");
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.");

        var (items, totalCount) = await repository.GetManagerChangeRequestsPagedAsync(
            manager.BranchId!.Value, status, pageNumber, pageSize, cancellationToken);

        var mapped = items.Select(MapToManagerChangeRequestResponse).ToList();
        return new PagedResponse<ManagerContractChangeRequestResponse>
        {
            Items = mapped,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    public async Task<ManagerContractChangeRequestResponse> GetChangeRequestByIdAsync(
        Guid managerId, int changeRequestId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (changeRequestId <= 0)
            throw new ArgumentException("ChangeRequestId must be greater than 0.");

        var cr = await repository.GetChangeRequestByIdAsync(changeRequestId, cancellationToken);
        if (cr == null)
            throw new KeyNotFoundException("Yêu cầu thay đổi hợp đồng không tồn tại.");
        if (cr.Contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Yêu cầu thay đổi không thuộc chi nhánh của quản lý.");

        return MapToManagerChangeRequestResponse(cr);
    }

    public async Task<ManagerContractChangeRequestResponse> ApproveChangeRequestAsync(
        Guid managerId, int changeRequestId, ApproveContractChangeRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (changeRequestId <= 0)
            throw new ArgumentException("ChangeRequestId must be greater than 0.");

        var cr = await repository.GetChangeRequestByIdAsync(changeRequestId, cancellationToken);
        if (cr == null)
            throw new KeyNotFoundException("Yêu cầu thay đổi hợp đồng không tồn tại.");
        if (cr.Contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Yêu cầu thay đổi không thuộc chi nhánh của quản lý.");
        if (cr.Status != "SUBMITTED")
            throw new InvalidOperationException("Chỉ có thể duyệt yêu cầu thay đổi ở trạng thái SUBMITTED.");

        var contract = cr.Contract;
        var now = DateTime.UtcNow;

        var managerItemOverrides = request.Items?
            .Where(i => !string.IsNullOrWhiteSpace(i.FieldName) && !string.IsNullOrWhiteSpace(i.FinalValue))
            .ToDictionary(i => i.FieldName.Trim().ToLowerInvariant(), i => i.FinalValue.Trim(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var appliedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in cr.ContractChangeRequestItems)
        {
            var cleanField = item.FieldName.Trim().ToLowerInvariant();
            var staffProposed = DeserializeJsonString(item.ProposedValue) ?? item.ProposedValue;
            var finalValStr = managerItemOverrides.TryGetValue(cleanField, out var mgrVal) ? mgrVal : staffProposed;

            switch (cleanField)
            {
                case "startdate":
                    if (DateOnly.TryParse(finalValStr, out var newStartDate))
                    {
                        if (contract.EndDate.HasValue && contract.EndDate.Value <= newStartDate)
                            throw new ArgumentException("Ngày kết thúc hợp đồng phải sau ngày bắt đầu.");
                        contract.StartDate = newStartDate;
                        appliedValues["StartDate"] = newStartDate.ToString("yyyy-MM-dd");
                    }
                    break;

                case "enddate":
                    if (string.IsNullOrWhiteSpace(finalValStr) || string.Equals(finalValStr, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        contract.EndDate = null;
                        appliedValues["EndDate"] = null;
                    }
                    else if (DateOnly.TryParse(finalValStr, out var newEndDate))
                    {
                        if (newEndDate <= contract.StartDate)
                            throw new ArgumentException("Ngày kết thúc hợp đồng phải sau ngày bắt đầu.");
                        contract.EndDate = newEndDate;
                        appliedValues["EndDate"] = newEndDate.ToString("yyyy-MM-dd");
                    }
                    break;

                case "hourlyrate":
                    if (decimal.TryParse(finalValStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var newRate) ||
                        decimal.TryParse(finalValStr, out newRate))
                    {
                        if (contract.Region?.RegionalMinimumWage != null && contract.Region.RegionalMinimumWage.Status == "ACTIVE")
                        {
                            var minWage = contract.Region.RegionalMinimumWage.MinimumHourlyRate;
                            if (minWage > 0 && newRate < minWage)
                                throw new ArgumentException($"Mức lương ({newRate:#,##0} đ/giờ) không được thấp hơn lương tối thiểu vùng ({minWage:#,##0} đ/giờ).");
                        }
                        contract.HourlyRate = newRate;
                        appliedValues["HourlyRate"] = newRate;
                    }
                    break;

                case "employeeposition" or "position":
                    if (!string.IsNullOrWhiteSpace(finalValStr))
                    {
                        if (finalValStr.Length > 100)
                            throw new ArgumentException("Chức danh / vị trí không được vượt quá 100 ký tự.");
                        contract.EmployeePositionSnapshot = finalValStr.Trim();
                        appliedValues["EmployeePosition"] = finalValStr.Trim();
                    }
                    break;

                case "contractterms" or "terms":
                    contract.ContractTerms = finalValStr.Trim();
                    appliedValues["ContractTerms"] = finalValStr.Trim();
                    break;
            }
        }

        var templateVersion = string.IsNullOrWhiteSpace(contract.ContractTemplateVersion) ? "1.0" : IncrementVersion(contract.ContractTemplateVersion);

        var oldPdfPath = contract.ContractFilePath;
        var officialPdfBytes = _pdfGenerator.GeneratePdf(contract, isPreview: false);
        string newPdfPublicId = oldPdfPath;

        if (_cloudinaryService != null)
        {
            try
            {
                newPdfPublicId = await _cloudinaryService.UploadContractPdfAsync(contract.ContractNumber, officialPdfBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Tải lên file PDF hợp đồng mới thất bại: {ex.Message}", ex);
            }
        }

        contract.ContractFilePath = newPdfPublicId;
        contract.ContractTemplateVersion = templateVersion;
        contract.PdfGeneratedAt = now;
        contract.SentToStaffAt = now;
        contract.StaffAcceptedAt = null;
        contract.Status = "PENDING_STAFF";
        contract.UpdatedAt = now;

        var auditDetails = new
        {
            contractId = contract.ContractId,
            changeRequestId = changeRequestId,
            fromStatus = "PENDING_MANAGER_APPROVAL",
            toStatus = "PENDING_STAFF",
            reviewComment = request.ReviewComment?.Trim(),
            contractTemplateVersion = templateVersion,
            oldPdfPath = oldPdfPath,
            newPdfPath = newPdfPublicId,
            appliedFinalValues = appliedValues,
            note = $"Quản lý đã duyệt yêu cầu thay đổi hợp đồng. Cập nhật phiên bản {templateVersion} và gửi cho nhân viên chấp thuận."
        };

        await repository.ApproveChangeRequestWithContractUpdateAsync(
            changeRequestId, managerId, request.ReviewComment?.Trim(), contract, auditDetails, cancellationToken);

        cr.Status = "APPROVED";
        cr.ReviewedBy = managerId;
        cr.ReviewedAt = now;
        cr.ReviewComment = request.ReviewComment?.Trim();

        try
        {
            var notification = new NotificationEntity
            {
                NotificationType = "SYSTEM",
                Title = "Yêu cầu thay đổi hợp đồng đã được phê duyệt",
                Content = $"Yêu cầu thay đổi hợp đồng {contract.ContractNumber} đã được quản lý phê duyệt. Hợp đồng phiên bản mới ({templateVersion}) đã được gửi cho bạn. Vui lòng xem xét và chấp thuận.",
                CreatedBy = managerId,
                TargetBranchId = contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { cr.RequestedBy }, cancellationToken);
        }
        catch
        {
            // Fail-safe notification logging
        }

        return MapToManagerChangeRequestResponse(cr);
    }

    public async Task<ManagerContractChangeRequestResponse> RejectChangeRequestAsync(
        Guid managerId, int changeRequestId, RejectContractChangeRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (changeRequestId <= 0)
            throw new ArgumentException("ChangeRequestId must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Lý do từ chối yêu cầu là bắt buộc.");

        var cr = await repository.GetChangeRequestByIdAsync(changeRequestId, cancellationToken);
        if (cr == null)
            throw new KeyNotFoundException("Yêu cầu thay đổi hợp đồng không tồn tại.");
        if (cr.Contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Yêu cầu thay đổi không thuộc chi nhánh của quản lý.");
        if (cr.Status != "SUBMITTED")
            throw new InvalidOperationException("Chỉ có thể từ chối yêu cầu thay đổi ở trạng thái SUBMITTED.");

        var reason = request.Reason.Trim();
        await repository.RejectChangeRequestAsync(changeRequestId, managerId, reason, cancellationToken);

        cr.Status = "REJECTED";
        cr.ReviewedBy = managerId;
        cr.ReviewedAt = DateTime.UtcNow;
        cr.ReviewComment = reason;

        try
        {
            var notification = new NotificationEntity
            {
                NotificationType = "SYSTEM",
                Title = "Yêu cầu thay đổi hợp đồng đã bị từ chối",
                Content = $"Yêu cầu thay đổi hợp đồng {cr.Contract.ContractNumber} đã bị quản lý từ chối. Lý do: {reason}.",
                CreatedBy = managerId,
                TargetBranchId = cr.Contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { cr.RequestedBy }, cancellationToken);
        }
        catch
        {
            // Fail-safe notification logging
        }

        return MapToManagerChangeRequestResponse(cr);
    }

    public async Task<ManagerContractHistoryResponse> GetContractHistoryAsync(
        Guid managerId, int contractId, int pageNumber = 1, int pageSize = 20, string? sortOrder = "desc", CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Hợp đồng lao động không tồn tại.");
        if (contract.BranchId != manager.BranchId!.Value)
            throw new UnauthorizedAccessException("Hợp đồng không thuộc chi nhánh của quản lý.");

        var successor = await repository.GetSuccessorContractAsync(contractId, cancellationToken);
        EmploymentContract? predecessor = null;
        if (contract.ReplacesContractId.HasValue)
        {
            predecessor = await repository.GetByIdAsync(contract.ReplacesContractId.Value, cancellationToken);
        }

        var (auditLogs, totalCount) = await repository.GetContractAuditLogsPagedAsync(contractId, pageNumber, pageSize, sortOrder, cancellationToken);

        var items = auditLogs.Select(MapAuditLogToEntry).ToList();

        return new ManagerContractHistoryResponse
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            CurrentStatus = contract.Status,
            StaffUserId = contract.UserId,
            StaffFullName = contract.EmployeeFullNameSnapshot,
            BranchId = contract.BranchId,
            BranchName = contract.BranchNameSnapshot,
            ReplacesContractId = contract.ReplacesContractId,
            ReplacesContractNumber = predecessor?.ContractNumber,
            ReplacedByContractId = successor?.ContractId,
            ReplacedByContractNumber = successor?.ContractNumber,
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    private static ContractHistoryEntryDto MapAuditLogToEntry(AuditLog log) =>
        EmploymentContractAuditLogMapper.MapAuditLogToEntry(log);

    public async Task<IReadOnlyList<StaffContractHistoryItemResponse>> GetStaffContractHistoryAsync(
        Guid managerId, Guid staffUserId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (staffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId is required.");

        var contracts = await repository.GetStaffContractsInBranchAsync(staffUserId, manager.BranchId!.Value, cancellationToken);
        return contracts.Select(c => new StaffContractHistoryItemResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            EmployeePosition = c.EmployeePositionSnapshot,
            ReplacesContractId = c.ReplacesContractId,
            CreatedAt = c.CreatedAt,
            SentToStaffAt = c.SentToStaffAt,
            StaffAcceptedAt = c.StaffAcceptedAt,
            TerminatedAt = c.TerminatedAt,
            TerminationReason = c.TerminationReason,
            TerminationEffectiveAt = c.TerminationEffectiveAt
        }).ToList();
    }

    private static decimal ValidateAndGetMinimumWage(Region? region, DateOnly startDate)
    {
        if (region == null || !region.IsActive)
            throw new ArgumentException("Branch region must be active.");
        var wage = region.RegionalMinimumWage;
        if (region.RegionalMinimumWageId == null || wage == null || wage.RegionalMinimumWageId != region.RegionalMinimumWageId)
            throw new ArgumentException("Region has no assigned minimum wage configuration.");
        if (wage.Status != "ACTIVE" || wage.MinimumHourlyRate <= 0 || wage.EffectiveFrom == null ||
            startDate < wage.EffectiveFrom || (wage.EffectiveTo != null && startDate > wage.EffectiveTo))
            throw new ArgumentException("Regional minimum wage must be ACTIVE and effective on the contract start date.");

        return wage.MinimumHourlyRate;
    }

    private async Task<User> RequireManagerAsync(Guid managerId, CancellationToken cancellationToken)
    {
        var manager = await repository.GetCreationUserAsync(managerId, cancellationToken);
        if (manager == null || manager.DeletedAt != null)
            throw new UnauthorizedAccessException("Manager does not exist.");
        if (!string.Equals(manager.Role?.RoleName, "MANAGER", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Actor does not have the MANAGER role.");
        if (manager.Status != "ACTIVE")
            throw new UnauthorizedAccessException("Manager account is not active.");
        if (!manager.BranchId.HasValue)
            throw new UnauthorizedAccessException("Manager must be assigned to an active branch.");
        return manager;
    }

    private static ManagerEmploymentContractListItemResponse MapToListItemResponse(EmploymentContract contract) =>
        new()
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            HourlyRate = contract.HourlyRate,
            EmployeePosition = contract.EmployeePositionSnapshot,
            BranchId = contract.BranchId,
            BranchName = contract.BranchNameSnapshot,
            RegionId = contract.RegionId,
            RegionName = contract.RegionNameSnapshot,
            StaffUserId = contract.UserId,
            EmployeeFullName = contract.EmployeeFullNameSnapshot,
            EmployeePhone = contract.EmployeePhoneSnapshot,
            EmployeeIdentityNo = contract.EmployeeIdentityNoSnapshot,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            TerminationReason = contract.TerminationReason,
            HasPdf = !string.IsNullOrWhiteSpace(contract.ContractFilePath)
        };

    private static ManagerEmploymentContractDetailResponse MapToDetailResponse(EmploymentContract contract) =>
        new()
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            HourlyRate = contract.HourlyRate,
            ContractTerms = contract.ContractTerms,
            ReplacesContractId = contract.ReplacesContractId,
            StaffUserId = contract.UserId,
            EmployeeFullName = contract.EmployeeFullNameSnapshot,
            EmployeePhone = contract.EmployeePhoneSnapshot,
            EmployeeAddress = contract.EmployeeAddressSnapshot,
            EmployeeIdentityNo = contract.EmployeeIdentityNoSnapshot,
            EmployeePosition = contract.EmployeePositionSnapshot,
            BranchId = contract.BranchId,
            BranchName = contract.BranchNameSnapshot,
            BranchAddress = contract.BranchAddressSnapshot,
            RegionId = contract.RegionId,
            RegionName = contract.RegionNameSnapshot,
            CreatedBy = contract.CreatedBy,
            CreatedByManagerName = contract.CreatedByNavigation?.FullName,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            SentToStaffAt = contract.SentToStaffAt,
            StaffAcceptedAt = contract.StaffAcceptedAt,
            TerminationReason = contract.TerminationReason,
            TerminationEffectiveAt = contract.TerminationEffectiveAt,
            TerminatedBy = contract.TerminatedBy,
            TerminatedAt = contract.TerminatedAt,
            CancellationReason = contract.CancellationReason,
            CancelledAt = contract.CancelledAt,
            CancelledBy = contract.CancelledBy,
            ManagerRejectionComment = contract.ManagerRejectionComment,
            HasPdf = !string.IsNullOrWhiteSpace(contract.ContractFilePath),
            ContractTemplateVersion = contract.ContractTemplateVersion,
            OpenChangeRequestId = contract.ContractChangeRequest?.ChangeRequestId,
            OpenChangeRequestStatus = contract.ContractChangeRequest?.Status
        };

    private static ManagerContractChangeRequestResponse MapToManagerChangeRequestResponse(ContractChangeRequest cr)
    {
        return new ManagerContractChangeRequestResponse
        {
            ChangeRequestId = cr.ChangeRequestId,
            ContractId = cr.ContractId,
            ContractNumber = cr.Contract?.ContractNumber ?? string.Empty,
            StaffUserId = cr.RequestedBy,
            StaffFullName = cr.RequestedByNavigation?.FullName ?? cr.Contract?.EmployeeFullNameSnapshot ?? string.Empty,
            RequestType = cr.RequestType,
            StaffComment = cr.StaffComment,
            Status = cr.Status,
            SubmittedAt = cr.SubmittedAt,
            ReviewedBy = cr.ReviewedBy,
            ReviewedByName = cr.ReviewedByNavigation?.FullName,
            ReviewedAt = cr.ReviewedAt,
            ReviewComment = cr.ReviewComment,
            CreatedAt = cr.CreatedAt,
            ContractStatus = cr.Contract?.Status,
            CurrentStartDate = cr.Contract?.StartDate,
            CurrentEndDate = cr.Contract?.EndDate,
            CurrentHourlyRate = cr.Contract?.HourlyRate,
            CurrentEmployeePosition = cr.Contract?.EmployeePositionSnapshot,
            CurrentContractTerms = cr.Contract?.ContractTerms,
            BranchName = cr.Contract?.Branch?.BranchName ?? cr.Contract?.BranchNameSnapshot,
            ContractTemplateVersion = cr.Contract?.ContractTemplateVersion,
            Items = cr.ContractChangeRequestItems.Select(i => new ContractChangeRequestItemDetailDto
            {
                ChangeRequestItemId = i.ChangeRequestItemId,
                FieldName = i.FieldName,
                OriginalValue = DeserializeJsonString(i.OriginalValue),
                ProposedValue = DeserializeJsonString(i.ProposedValue) ?? i.ProposedValue,
                Reason = i.Reason
            }).ToList()
        };
    }

    private static string? DeserializeJsonString(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<string>(json);
        }
        catch
        {
            return json;
        }
    }

    private static string IncrementVersion(string currentVersion)
    {
        if (decimal.TryParse(currentVersion, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ver))
        {
            return (ver + 0.1m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
        return "1.1";
    }
}
