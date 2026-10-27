using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class AdminEmployeeRequestService : IAdminEmployeeRequestService
{
    private readonly IAdminEmployeeRequestRepo _repository;
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private static readonly HashSet<string> SupportedRequestTypes = new(StringComparer.OrdinalIgnoreCase)
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

    public AdminEmployeeRequestService(IAdminEmployeeRequestRepo repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<AdminEmployeeRequestListItemResponse>> GetPagedAsync(
        AdminEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
        {
            throw new ArgumentException("PageNumber must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        string? normalizedRequestType = null;
        if (!string.IsNullOrWhiteSpace(query.RequestType))
        {
            var trimmedType = query.RequestType.Trim().ToUpperInvariant();
            if (!SupportedRequestTypes.Contains(trimmedType))
            {
                throw new ArgumentException("Invalid requestType. Allowed values are LEAVE, LATE_ARRIVAL, EARLY_LEAVE, SALARY_INCREASE.");
            }
            normalizedRequestType = trimmedType;
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var trimmedStatus = query.Status.Trim().ToUpperInvariant();
            if (!AllowedStatuses.Contains(trimmedStatus))
            {
                throw new ArgumentException("Invalid status. Allowed values are PENDING, APPROVED, REJECTED, CANCELLED.");
            }
            normalizedStatus = trimmedStatus;
        }

        if (query.BranchId.HasValue && query.BranchId.Value <= 0)
        {
            throw new ArgumentException("BranchId must be greater than 0.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            throw new ArgumentException("FromDate cannot be later than ToDate.");
        }

        DateTime? fromUtc = null;
        if (query.FromDate.HasValue)
        {
            fromUtc = ConvertVnDateToUtcStartOfDay(query.FromDate.Value);
        }

        DateTime? toUtc = null;
        if (query.ToDate.HasValue)
        {
            toUtc = ConvertVnDateToUtcStartOfNextDay(query.ToDate.Value);
        }

        var (items, totalCount) = await _repository.GetPagedAsync(
            query.Search,
            normalizedRequestType,
            normalizedStatus,
            query.BranchId,
            query.UserId,
            fromUtc,
            toUtc,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var mappedItems = items.Select(r =>
        {
            AdminRequestShiftSummaryResponse? shiftSummary = null;
            if (string.Equals(r.RequestType, "LEAVE", StringComparison.OrdinalIgnoreCase))
            {
                var assignments = r.LeaveRequestAssignments
                    .Where(l => l.Assignment?.Shift != null)
                    .OrderBy(l => l.Assignment!.Shift!.ScheduledStartAt)
                    .ToList();
                if (assignments.Count > 0)
                {
                    var firstShift = assignments[0].Assignment!.Shift!;
                    shiftSummary = new AdminRequestShiftSummaryResponse
                    {
                        TotalShifts = assignments.Count,
                        PrimaryShiftName = firstShift.ShiftNameSnapshot ?? firstShift.ShiftTemplate?.ShiftName,
                        PrimaryScheduledStartAt = ToUtc(firstShift.ScheduledStartAt),
                        PrimaryScheduledEndAt = ToUtc(firstShift.ScheduledEndAt)
                    };
                }
            }
            else if (r.TimeAdjustmentRequestDetail?.Assignment?.Shift != null)
            {
                var tard = r.TimeAdjustmentRequestDetail;
                var shift = tard.Assignment!.Shift!;
                shiftSummary = new AdminRequestShiftSummaryResponse
                {
                    TotalShifts = 1,
                    PrimaryShiftName = shift.ShiftNameSnapshot ?? shift.ShiftTemplate?.ShiftName,
                    PrimaryScheduledStartAt = ToUtc(shift.ScheduledStartAt),
                    PrimaryScheduledEndAt = ToUtc(shift.ScheduledEndAt),
                    RequestedTime = ToUtc(tard.RequestedTime)
                };
            }

            return new AdminEmployeeRequestListItemResponse
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
                SalaryIncreaseDetail = r.SalaryIncreaseRequestDetail == null ? null : new AdminSalaryIncreaseDetailResponse
                {
                    CurrentContractId = r.SalaryIncreaseRequestDetail.CurrentContractId,
                    ContractNumber = r.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber,
                    CurrentHourlyRate = r.SalaryIncreaseRequestDetail.CurrentHourlyRate,
                    ProposedHourlyRate = r.SalaryIncreaseRequestDetail.ProposedHourlyRate
                },
                RequestType = r.RequestType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = ToUtc(r.CreatedAt),
                ReviewedAt = ToUtc(r.ReviewedAt),
                ReviewerFullName = r.ReviewedByNavigation?.FullName,
                ShiftSummary = shiftSummary
            };
        }).ToList();

        return new PagedResponse<AdminEmployeeRequestListItemResponse>
        {
            Items = mappedItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalItems = totalCount
        };
    }

    public async Task<AdminEmployeeRequestDetailResponse> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestId <= 0)
        {
            throw new ArgumentException("RequestId must be greater than 0.");
        }

        var request = await _repository.GetByIdAsync(requestId, cancellationToken);
        if (request == null || !SupportedRequestTypes.Contains(request.RequestType))
        {
            throw new KeyNotFoundException($"Employee request with ID {requestId} was not found.");
        }

        var response = new AdminEmployeeRequestDetailResponse
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
            SalaryIncreaseDetail = request.SalaryIncreaseRequestDetail == null ? null : new AdminSalaryIncreaseDetailResponse
            {
                CurrentContractId = request.SalaryIncreaseRequestDetail.CurrentContractId,
                ContractNumber = request.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber,
                CurrentHourlyRate = request.SalaryIncreaseRequestDetail.CurrentHourlyRate,
                ProposedHourlyRate = request.SalaryIncreaseRequestDetail.ProposedHourlyRate
            },
            RequestType = request.RequestType,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAt = ToUtc(request.CreatedAt),
            UpdatedAt = ToUtc(request.UpdatedAt),
            ReviewedBy = request.ReviewedBy,
            ReviewerFullName = request.ReviewedByNavigation?.FullName,
            ReviewedAt = ToUtc(request.ReviewedAt),
            ReviewNote = request.ReviewNote
        };

        if (request.AppliedRequestLimitRule != null)
        {
            response.AppliedRule = new AdminAppliedRequestLimitRuleResponse
            {
                RuleId = request.AppliedRequestLimitRule.RequestLimitRuleId,
                RequestType = request.AppliedRequestLimitRule.RequestType,
                MaxRequestsPerMonth = request.AppliedRequestLimitRule.MaxRequestsPerMonth,
                MinimumAdvanceDays = request.AppliedRequestLimitRule.MinimumAdvanceDays,
                SubmissionDeadlineDayOfWeek = request.AppliedRequestLimitRule.SubmissionDeadlineDayOfWeek,
                EffectiveFrom = request.AppliedRequestLimitRule.EffectiveFrom,
                EffectiveTo = request.AppliedRequestLimitRule.EffectiveTo,
                Status = request.AppliedRequestLimitRule.Status
            };
        }

        if (string.Equals(request.RequestType, "LEAVE", StringComparison.OrdinalIgnoreCase))
        {
            response.LeaveAssignments = request.LeaveRequestAssignments
                .Select(lra => new AdminLeaveRequestAssignmentResponse
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
            response.TimeAdjustmentDetail = new AdminTimeAdjustmentDetailResponse
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

    public async Task<IReadOnlyList<AdminEmployeeRequestHistoryResponse>> GetHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var auditLogs = await _repository.GetHistoryAsync(requestId, cancellationToken);

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

            return new AdminEmployeeRequestHistoryResponse
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

    private static DateTime ToUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    private static DateTime? ToUtc(DateTime? dt) => dt.HasValue ? ToUtc(dt.Value) : null;

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
