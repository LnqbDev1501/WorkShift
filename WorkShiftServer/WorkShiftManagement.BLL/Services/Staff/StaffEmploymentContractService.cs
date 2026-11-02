using WorkShiftManagement.BLL.Interfaces.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.BLL.Services.Staff;

public class StaffEmploymentContractService(
    IEmploymentContractRepo repository,
    ICloudinaryService? cloudinaryService = null,
    INotificationRealtimePublisher? realtimePublisher = null,
    INotificationEmailSender? emailSender = null,
    ILogger<StaffEmploymentContractService>? logger = null,
    IConfiguration? configuration = null,
    IAdminRealtimePublisher? adminRealtimePublisher = null) : IStaffEmploymentContractService
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

        if (_cloudinaryService == null)
            throw new InvalidOperationException("Cloudinary service is not configured.");

        var bytes = await _cloudinaryService.DownloadRawFileAsync(contract.ContractFilePath);
        var fileName = $"{contract.ContractNumber}.pdf";
        return (bytes, "application/pdf", fileName);
    }

    public async Task<IReadOnlyList<StaffContractListItemResponse>> GetMyContractsAsync(Guid staffUserId, StaffContractListQuery? query = null, CancellationToken cancellationToken = default)
    {
        var contracts = await repository.GetStaffContractsAsync(
            staffUserId,
            query?.Status,
            query?.Search,
            query?.StartDateFrom,
            query?.StartDateTo,
            cancellationToken);

        return contracts.Select(c => new StaffContractListItemResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            HourlyRate = c.HourlyRate,
            EmployeeFullName = !string.IsNullOrWhiteSpace(c.EmployeeFullNameSnapshot)
                ? c.EmployeeFullNameSnapshot
                : (!string.IsNullOrWhiteSpace(c.User?.FullName) ? c.User.FullName : string.Empty),
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

        var todayVn = GetCurrentVietnamDate();
        bool isPending = string.Equals(contract.Status, "PENDING_STAFF", StringComparison.OrdinalIgnoreCase);
        bool isPastStart = todayVn > contract.StartDate;

        bool canAccept = isPending && !isPastStart;
        string? cannotAcceptReason = null;
        if (isPending && isPastStart)
        {
            cannotAcceptReason = "The contract start date has passed. The contract can no longer be accepted.";
        }

        var openCr = await repository.GetOpenChangeRequestByContractIdAsync(contractId, cancellationToken);
        bool isContractChangeable = isPending;
        bool canSubmitChangeRequest = isContractChangeable && openCr == null;
        string? cannotSubmitChangeRequestReason = null;
        if (openCr != null)
        {
            cannotSubmitChangeRequestReason = "This contract already has a change request under review.";
        }
        else if (!isContractChangeable)
        {
            cannotSubmitChangeRequestReason = "Only contracts awaiting acceptance (PENDING_STAFF) can have change requests. ACTIVE contracts cannot be changed through this process.";
        }

        return new StaffContractDetailResponse
        {
            ContractId = contract.ContractId,
            ContractNumber = contract.ContractNumber,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            HourlyRate = contract.HourlyRate,
            EmployeeFullName = !string.IsNullOrWhiteSpace(contract.EmployeeFullNameSnapshot)
                ? contract.EmployeeFullNameSnapshot
                : (!string.IsNullOrWhiteSpace(contract.User?.FullName) ? contract.User.FullName : string.Empty),
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
            CannotAcceptReason = cannotAcceptReason,
            CanSubmitChangeRequest = canSubmitChangeRequest,
            CannotSubmitChangeRequestReason = cannotSubmitChangeRequestReason,
            OpenChangeRequestId = openCr?.ChangeRequestId,
            OpenChangeRequestStatus = openCr?.Status,
            TerminationReason = contract.TerminationReason,
            TerminationEffectiveAt = contract.TerminationEffectiveAt,
            CancellationReason = contract.CancellationReason,
            CancelledAt = contract.CancelledAt
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

        var todayVn = GetCurrentVietnamDate();
        if (todayVn > contract.StartDate)
        {
            throw new InvalidOperationException("The contract start date has passed. The contract can no longer be accepted.");
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
                Title = "Employee accepted contract",
                Content = $"{contract.EmployeeFullNameSnapshot} accepted employment contract {contract.ContractNumber}. Contract status: {newStatus}.",
                CreatedBy = staffUserId,
                TargetBranchId = contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
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

    public Task<IReadOnlyList<string>> GetActivePositionNamesAsync(CancellationToken cancellationToken = default) =>
        repository.GetActivePositionNamesAsync(cancellationToken);

    public async Task<StaffContractChangeRequestResponse> SubmitChangeRequestAsync(Guid staffUserId, int contractId, SubmitContractChangeRequestRequest request, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("At least one field change is required.");

        if (request.Items.GroupBy(x => x.FieldName.Trim().ToLowerInvariant()).Any(g => g.Count() > 1))
            throw new ArgumentException("Each field can appear only once in a change request.");

        var contract = await repository.GetStaffContractByIdAsync(contractId, staffUserId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("The contract was not found or does not belong to you.");

        var allowedStatuses = new[] { "PENDING_STAFF" };
        if (!allowedStatuses.Contains(contract.Status, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot request changes to a contract with status {contract.Status}. Only PENDING_STAFF contracts can have change requests.");

        var existingCr = await repository.GetOpenChangeRequestByContractIdAsync(contractId, cancellationToken);
        if (existingCr != null)
            throw new InvalidOperationException("This contract already has an unresolved change request.");

        var now = DateTime.UtcNow;
        var cleanRequestType = (!string.IsNullOrWhiteSpace(request.RequestType) &&
            string.Equals(request.RequestType.Trim(), "ACCEPTANCE", StringComparison.OrdinalIgnoreCase))
            ? "ACCEPTANCE"
            : "CHANGE_REQUEST";

        var changeRequest = new ContractChangeRequest
        {
            ContractId = contractId,
            RequestedBy = staffUserId,
            RequestType = cleanRequestType,
            StaffComment = request.StaffComment?.Trim(),
            Status = "SUBMITTED",
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var item in request.Items)
        {
            string cleanField = item.FieldName.Trim();
            if (string.Equals(cleanField, "EmployeePosition", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cleanField, "Position", StringComparison.OrdinalIgnoreCase))
            {
                var validPositions = await repository.GetActivePositionNamesAsync(cancellationToken);
                var isMatched = validPositions.Any(p => string.Equals(p, item.ProposedValue.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!isMatched)
                {
                    throw new ArgumentException($"Position '{item.ProposedValue}' is invalid or is not an active position.");
                }
            }

            string? originalValue = cleanField.ToLowerInvariant() switch
            {
                "startdate" => contract.StartDate.ToString("yyyy-MM-dd"),
                "enddate" => contract.EndDate?.ToString("yyyy-MM-dd"),
                "employeeposition" or "position" => contract.EmployeePositionSnapshot,
                "contractterms" or "terms" => contract.ContractTerms,
                "hourlyrate" => contract.HourlyRate.ToString("F2"),
                _ => null
            };

            changeRequest.ContractChangeRequestItems.Add(new ContractChangeRequestItem
            {
                FieldName = cleanField,
                OriginalValue = originalValue != null ? JsonSerializer.Serialize(originalValue) : null,
                ProposedValue = JsonSerializer.Serialize(item.ProposedValue.Trim()),
                Reason = item.Reason?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await repository.CreateChangeRequestAsync(changeRequest, contract.Status, cancellationToken);

        // Notify Manager
        try
        {
            var notification = new WorkShiftManagement.DAL.Entities.Notification
            {
                NotificationType = "SYSTEM",
                Title = "Employee contract change request",
                Content = $"{contract.EmployeeFullNameSnapshot} requested changes to contract {contract.ContractNumber}. Please review and respond.",
                CreatedBy = staffUserId,
                TargetBranchId = contract.BranchId,
                TargetType = "Branch",
                Channels = new() { "InApp" },
                TargetRoles = new(),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };
            await repository.AddNotificationAsync(notification, new[] { contract.CreatedBy }, cancellationToken);
        }
        catch
        {
            // Fail-safe logging
        }

        return MapToChangeRequestResponse(changeRequest);
    }

    public async Task<IReadOnlyList<StaffContractChangeRequestResponse>> GetMyContractChangeRequestsAsync(Guid staffUserId, int contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("ContractId must be greater than 0.");

        var contract = await repository.GetByIdAsync(contractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Employment contract not found.");

        if (contract.UserId != staffUserId)
            throw new UnauthorizedAccessException("You can only view your own contract change requests.");

        var list = await repository.GetChangeRequestsByContractIdAsync(contractId, cancellationToken);
        return list.Select(MapToChangeRequestResponse).ToList();
    }

    public async Task<StaffContractChangeRequestResponse> GetMyChangeRequestByIdAsync(Guid staffUserId, int changeRequestId, CancellationToken cancellationToken = default)
    {
        if (changeRequestId <= 0)
            throw new ArgumentException("ChangeRequestId must be greater than 0.");

        var cr = await repository.GetChangeRequestByIdAsync(changeRequestId, cancellationToken);
        if (cr == null)
            throw new KeyNotFoundException("Contract change request not found.");

        if (cr.RequestedBy != staffUserId && cr.Contract.UserId != staffUserId)
            throw new UnauthorizedAccessException("You can only view your own contract change requests.");

        return MapToChangeRequestResponse(cr);
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
            throw new InvalidOperationException("Shift registration requires an ACTIVE contract for the shift's branch covering the entire shift. SCHEDULED or PENDING contracts do not allow registration.");
        }
    }

    public async Task<int> ActivateDueScheduledContractsAsync(CancellationToken cancellationToken = default)
    {
        var todayVn = GetCurrentVietnamDate();
        var count = await repository.ActivateDueScheduledContractsAsync(todayVn, cancellationToken);
        await PublishContractChangesAsync(count, "ACTIVATED");
        return count;
    }

    public async Task<int> ExpireDueContractsAsync(CancellationToken cancellationToken = default)
    {
        var todayVn = GetCurrentVietnamDate();
        var count = await repository.ExpireDueContractsAsync(todayVn, cancellationToken);
        await PublishContractChangesAsync(count, "EXPIRED");
        return count;
    }

    public async Task<int> CancelOverduePendingContractsAsync(CancellationToken cancellationToken = default)
    {
        var todayVn = GetCurrentVietnamDate();
        var result = await repository.CancelOverduePendingContractsAsync(todayVn, cancellationToken);
        await PublishContractChangesAsync(result.CancelledCount, "AUTO_CANCELLED");

        if (result.Dispatches != null && result.Dispatches.Count > 0)
        {
            var isEmailEnabled = configuration == null ||
                (!string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]) &&
                 !string.IsNullOrWhiteSpace(configuration["Resend:NotificationTemplateId"]));

            foreach (var dispatch in result.Dispatches)
            {
                // 1. Push Realtime SignalR notification
                if (realtimePublisher != null)
                {
                    var realtimeNotif = new RealtimeNotificationResponse
                    {
                        NotificationId = dispatch.NotificationId,
                        Title = dispatch.Title,
                        Message = dispatch.Content,
                        CreatedAt = dispatch.CreatedAt
                    };

                    foreach (var targetUserId in dispatch.RecipientUserIds)
                    {
                        try
                        {
                            await realtimePublisher.PublishAsync(targetUserId, realtimeNotif);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to publish SignalR notification to user {UserId} for contract {ContractNumber}",
                                targetUserId, dispatch.ContractNumber);
                        }
                    }
                }

                // 2. Send email via Resend if email channel enabled and configured
                if (emailSender != null && dispatch.HasEmailChannel && isEmailEnabled)
                {
                    foreach (var email in dispatch.RecipientEmails)
                    {
                        try
                        {
                            await emailSender.SendAsync(email, dispatch.Title, dispatch.Content);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to send Resend email to {Email} for contract {ContractNumber}",
                                email, dispatch.ContractNumber);
                        }
                    }
                }
            }
        }

        return result.CancelledCount;
    }

    private async Task PublishContractChangesAsync(int count, string action)
    {
        if (count > 0 && adminRealtimePublisher != null)
            await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                new { resource = "EmploymentContracts", action });
    }

    private static DateOnly GetCurrentVietnamDate()
    {
        var utcNow = DateTimeOffset.UtcNow;
        TimeZoneInfo vnTimeZone;
        try
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        var vnTime = TimeZoneInfo.ConvertTime(utcNow, vnTimeZone);
        return DateOnly.FromDateTime(vnTime.DateTime);
    }

    private static StaffContractChangeRequestResponse MapToChangeRequestResponse(ContractChangeRequest cr)
    {
        return new StaffContractChangeRequestResponse
        {
            ChangeRequestId = cr.ChangeRequestId,
            ContractId = cr.ContractId,
            RequestType = cr.RequestType,
            StaffComment = cr.StaffComment,
            Status = cr.Status,
            SubmittedAt = cr.SubmittedAt,
            ReviewComment = cr.ReviewComment,
            ReviewedAt = cr.ReviewedAt,
            CreatedAt = cr.CreatedAt,
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
}
