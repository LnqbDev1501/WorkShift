using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerEmployeeRequestService : IManagerEmployeeRequestService
{
    private readonly IManagerEmployeeRequestRepo _repo;

    private static readonly HashSet<string> AllowedRequestTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LEAVE",
        "LATE_ARRIVAL",
        "EARLY_LEAVE",
        "SALARY_INCREASE"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PENDING",
        "APPROVED",
        "REJECTED",
        "CANCELLED"
    };

    public ManagerEmployeeRequestService(IManagerEmployeeRequestRepo repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<ManagerEmployeeRequestListItemResponse>> GetPagedAsync(
        Guid managerId,
        ManagerEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var branchId = await GetActiveBranchIdAsync(managerId, cancellationToken);

        if (query.PageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.");
        }

        string? normalizedRequestType = null;
        if (!string.IsNullOrWhiteSpace(query.RequestType))
        {
            var trimmed = query.RequestType.Trim().ToUpperInvariant();
            if (!AllowedRequestTypes.Contains(trimmed))
            {
                throw new ArgumentException($"Invalid request type '{query.RequestType}'. Allowed types: LEAVE, LATE_ARRIVAL, EARLY_LEAVE, SALARY_INCREASE.");
            }
            normalizedRequestType = trimmed;
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var trimmed = query.Status.Trim().ToUpperInvariant();
            if (!AllowedStatuses.Contains(trimmed))
            {
                throw new ArgumentException($"Invalid status '{query.Status}'. Allowed statuses: PENDING, APPROVED, REJECTED, CANCELLED.");
            }
            normalizedStatus = trimmed;
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            throw new ArgumentException("From date cannot be after to date.");
        }

        DateTime? fromUtc = query.FromDate.HasValue
            ? ConvertVnDateToUtcStartOfDay(query.FromDate.Value)
            : null;

        DateTime? toUtc = query.ToDate.HasValue
            ? ConvertVnDateToUtcStartOfNextDay(query.ToDate.Value)
            : null;

        var (items, totalCount) = await _repo.GetPagedAsync(
            branchId,
            query.Search,
            normalizedRequestType,
            normalizedStatus,
            query.UserId,
            fromUtc,
            toUtc,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var responseItems = items.Select(r =>
        {
            ManagerRequestShiftSummaryResponse? shiftSummary = null;
            if (r.RequestType == "LEAVE" && r.LeaveRequestAssignments != null && r.LeaveRequestAssignments.Count > 0)
            {
                var firstLeave = r.LeaveRequestAssignments
                    .Where(l => l.Assignment?.Shift != null)
                    .OrderBy(l => l.Assignment.Shift.ScheduledStartAt)
                    .FirstOrDefault();

                if (firstLeave != null)
                {
                    shiftSummary = new ManagerRequestShiftSummaryResponse
                    {
                        TotalShifts = r.LeaveRequestAssignments.Count,
                        PrimaryShiftName = firstLeave.Assignment.Shift.ShiftNameSnapshot,
                        PrimaryScheduledStartAt = ToUtc(firstLeave.Assignment.Shift.ScheduledStartAt),
                        PrimaryScheduledEndAt = ToUtc(firstLeave.Assignment.Shift.ScheduledEndAt)
                    };
                }
            }
            else if ((r.RequestType is "LATE_ARRIVAL" or "EARLY_LEAVE") && r.TimeAdjustmentRequestDetail != null)
            {
                var tard = r.TimeAdjustmentRequestDetail;
                var shift = tard.Assignment?.Shift;
                shiftSummary = new ManagerRequestShiftSummaryResponse
                {
                    TotalShifts = 1,
                    PrimaryShiftName = shift?.ShiftNameSnapshot,
                    PrimaryScheduledStartAt = shift != null ? ToUtc(shift.ScheduledStartAt) : null,
                    PrimaryScheduledEndAt = shift != null ? ToUtc(shift.ScheduledEndAt) : null,
                    RequestedTime = ToUtc(tard.RequestedTime)
                };
            }

            return new ManagerEmployeeRequestListItemResponse
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                EmployeeFullName = r.User.FullName,
                EmployeeEmail = r.User.Email,
                EmployeePhone = r.User.Phone,
                EmployeeAvatarUrl = r.User.AvatarUrl,
                BranchId = r.BranchId,
                BranchName = r.Branch.BranchName,
                BranchCode = r.Branch.BranchCode,
                SalaryIncreaseDetail = r.SalaryIncreaseRequestDetail == null ? null : new ManagerSalaryIncreaseDetailResponse { CurrentContractId = r.SalaryIncreaseRequestDetail.CurrentContractId, ContractNumber = r.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber, CurrentHourlyRate = r.SalaryIncreaseRequestDetail.CurrentHourlyRate, ProposedHourlyRate = r.SalaryIncreaseRequestDetail.ProposedHourlyRate },
                RequestType = r.RequestType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = ToUtc(r.CreatedAt),
                ReviewedBy = r.ReviewedBy,
                ReviewerFullName = r.ReviewedByNavigation?.FullName,
                ReviewedAt = ToUtc(r.ReviewedAt),
                ReviewNote = r.ReviewNote,
                ShiftSummary = shiftSummary
            };
        }).ToList();

        return new PagedResponse<ManagerEmployeeRequestListItemResponse>
        {
            Items = responseItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalItems = totalCount
        };
    }

    public async Task<ManagerEmployeeRequestDetailResponse> GetByIdAsync(
        Guid managerId,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var branchId = await GetActiveBranchIdAsync(managerId, cancellationToken);

        var request = await _repo.GetByIdAsync(requestId, branchId, cancellationToken);
        if (request == null)
        {
            throw new KeyNotFoundException("Request not found in your branch.");
        }

        return MapToDetailResponse(request);
    }

    public async Task<IReadOnlyList<ManagerEmployeeRequestHistoryResponse>> GetHistoryAsync(
        Guid managerId,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var branchId = await GetActiveBranchIdAsync(managerId, cancellationToken);

        var auditLogs = await _repo.GetHistoryAsync(requestId, branchId, cancellationToken);

        return auditLogs.Select(a =>
        {
            string? previousStatus = null;
            string? newStatus = null;
            string? reviewNote = null;

            if (!string.IsNullOrWhiteSpace(a.OldValue))
            {
                try
                {
                    using var oldDoc = JsonDocument.Parse(a.OldValue);
                    if (oldDoc.RootElement.TryGetProperty("Status", out var pStatus) && pStatus.ValueKind == JsonValueKind.String)
                    {
                        previousStatus = pStatus.GetString();
                    }
                }
                catch
                {
                    // Ignore JSON parse errors for backward compatibility
                }
            }

            if (!string.IsNullOrWhiteSpace(a.NewValue))
            {
                try
                {
                    using var newDoc = JsonDocument.Parse(a.NewValue);
                    if (newDoc.RootElement.TryGetProperty("Status", out var nStatus) && nStatus.ValueKind == JsonValueKind.String)
                    {
                        newStatus = nStatus.GetString();
                    }

                    if (newDoc.RootElement.TryGetProperty("ReviewNote", out var noteProp) && noteProp.ValueKind == JsonValueKind.String)
                    {
                        reviewNote = noteProp.GetString();
                    }
                    else if (newDoc.RootElement.TryGetProperty("Reason", out var reasonProp) && reasonProp.ValueKind == JsonValueKind.String)
                    {
                        reviewNote = reasonProp.GetString();
                    }
                }
                catch
                {
                    // Ignore JSON parse errors for backward compatibility
                }
            }

            return new ManagerEmployeeRequestHistoryResponse
            {
                AuditLogId = a.AuditLogId,
                RequestId = requestId,
                Action = a.Action,
                PerformedByUserId = a.UserId,
                PerformedByName = a.User?.FullName,
                RoleName = a.User?.Role?.RoleName,
                PerformedAt = ToUtc(a.CreatedAt),
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ReviewNote = reviewNote
            };
        }).ToList();
    }

    public async Task<ManagerEmployeeRequestDetailResponse> ApproveAsync(
        Guid managerId,
        int requestId,
        ApproveEmployeeRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var branchId = await GetActiveBranchIdAsync(managerId, cancellationToken);

        await _repo.ApproveRequestAsync(requestId, branchId, managerId, request.ReviewNote, cancellationToken);

        var updated = await _repo.GetByIdAsync(requestId, branchId, cancellationToken);
        if (updated == null)
        {
            throw new KeyNotFoundException("Request not found after approval.");
        }

        return MapToDetailResponse(updated);
    }

    public async Task<ManagerEmployeeRequestDetailResponse> RejectAsync(
        Guid managerId,
        int requestId,
        RejectEmployeeRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(request.ReviewNote))
        {
            throw new ArgumentException("Review note is required when rejecting a request.");
        }

        var branchId = await GetActiveBranchIdAsync(managerId, cancellationToken);

        await _repo.RejectRequestAsync(requestId, branchId, managerId, request.ReviewNote, cancellationToken);

        var updated = await _repo.GetByIdAsync(requestId, branchId, cancellationToken);
        if (updated == null)
        {
            throw new KeyNotFoundException("Request not found after rejection.");
        }

        return MapToDetailResponse(updated);
    }

    private async Task<int> GetActiveBranchIdAsync(Guid managerId, CancellationToken cancellationToken)
    {
        if (managerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid authenticated user ID is required.");
        }

        var manager = await _repo.GetActiveManagerAsync(managerId, cancellationToken);
        if (manager == null)
        {
            throw new UnauthorizedAccessException("Manager account not found or deactivated.");
        }

        if (manager.Role?.RoleName != "MANAGER" || manager.Role?.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("User does not have an active Manager role.");
        }

        if (!manager.BranchId.HasValue || manager.Branch == null || manager.Branch.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Manager is not assigned to an active branch.");
        }

        return manager.BranchId.Value;
    }

    private static ManagerEmployeeRequestDetailResponse MapToDetailResponse(EmployeeRequest request)
    {
        var response = new ManagerEmployeeRequestDetailResponse
        {
            RequestId = request.RequestId,
            UserId = request.UserId,
            EmployeeFullName = request.User.FullName,
            EmployeeEmail = request.User.Email,
            EmployeePhone = request.User.Phone,
            EmployeeAvatarUrl = request.User.AvatarUrl,
            BranchId = request.BranchId,
            BranchName = request.Branch.BranchName,
            BranchCode = request.Branch.BranchCode,
            SalaryIncreaseDetail = request.SalaryIncreaseRequestDetail == null ? null : new ManagerSalaryIncreaseDetailResponse { CurrentContractId = request.SalaryIncreaseRequestDetail.CurrentContractId, ContractNumber = request.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber, CurrentHourlyRate = request.SalaryIncreaseRequestDetail.CurrentHourlyRate, ProposedHourlyRate = request.SalaryIncreaseRequestDetail.ProposedHourlyRate },
            RequestType = request.RequestType,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAt = ToUtc(request.CreatedAt),
            UpdatedAt = ToUtc(request.UpdatedAt),
            ReviewedBy = request.ReviewedBy,
            ReviewerFullName = request.ReviewedByNavigation?.FullName,
            ReviewedAt = ToUtc(request.ReviewedAt),
            ReviewNote = request.ReviewNote,
            AppliedRule = request.AppliedRequestLimitRule != null
                ? new ManagerAppliedRequestLimitRuleResponse
                {
                    RuleId = request.AppliedRequestLimitRule.RequestLimitRuleId,
                    RequestType = request.AppliedRequestLimitRule.RequestType,
                    MaxRequestsPerMonth = request.AppliedRequestLimitRule.MaxRequestsPerMonth
                }
                : null
        };

        if (request.RequestType == "LEAVE")
        {
            response.LeaveAssignments = request.LeaveRequestAssignments
                .Select(lra => new ManagerLeaveRequestAssignmentResponse
                {
                    LeaveRequestAssignmentId = lra.LeaveRequestAssignmentId,
                    AssignmentId = lra.AssignmentId,
                    AssignmentStatus = lra.Assignment.Status,
                    ShiftId = lra.Assignment.Shift.ShiftId,
                    ShiftName = lra.Assignment.Shift.ShiftNameSnapshot,
                    ShiftTemplateName = lra.Assignment.Shift.ShiftTemplate?.ShiftName,
                    ScheduledStartAt = ToUtc(lra.Assignment.Shift.ScheduledStartAt),
                    ScheduledEndAt = ToUtc(lra.Assignment.Shift.ScheduledEndAt),
                    BranchName = lra.Assignment.Shift.ScheduleWeek?.Branch?.BranchName
                })
                .OrderBy(a => a.ScheduledStartAt)
                .ThenBy(a => a.AssignmentId)
                .ToList();
        }
        else if (request.TimeAdjustmentRequestDetail != null)
        {
            var tard = request.TimeAdjustmentRequestDetail;
            response.TimeAdjustmentDetail = new ManagerTimeAdjustmentDetailResponse
            {
                AssignmentId = tard.AssignmentId,
                RequestedTime = ToUtc(tard.RequestedTime),
                AssignmentStatus = tard.Assignment?.Status ?? string.Empty,
                ShiftId = tard.Assignment?.Shift?.ShiftId ?? 0,
                ShiftName = tard.Assignment?.Shift?.ShiftNameSnapshot ?? string.Empty,
                ShiftTemplateName = tard.Assignment?.Shift?.ShiftTemplate?.ShiftName,
                ScheduledStartAt = ToUtc(tard.Assignment?.Shift?.ScheduledStartAt ?? default),
                ScheduledEndAt = ToUtc(tard.Assignment?.Shift?.ScheduledEndAt ?? default),
                BranchName = tard.Assignment?.Shift?.ScheduleWeek?.Branch?.BranchName
            };
        }

        return response;
    }

    private static DateTime ToUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    private static DateTime? ToUtc(DateTime? dt) => dt.HasValue ? ToUtc(dt.Value) : null;

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private static DateTime ConvertVnDateToUtcStartOfDay(DateOnly date)
    {
        var vnDateTime = date.ToDateTime(TimeOnly.MinValue);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vnDateTime, DateTimeKind.Unspecified), VietnamTimeZone);
    }

    private static DateTime ConvertVnDateToUtcStartOfNextDay(DateOnly date)
    {
        var vnDateTime = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vnDateTime, DateTimeKind.Unspecified), VietnamTimeZone);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "Indochina Time", "Indochina Time");
            }
        }
    }
}
