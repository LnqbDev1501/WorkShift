using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.DTOs.Staff.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Staff;
using NotificationEntity = WorkShiftManagement.DAL.Entities.Notification;

namespace WorkShiftManagement.BLL.Services.Staff;

public class StaffEmployeeRequestService : IStaffEmployeeRequestService
{
    private readonly IStaffEmployeeRequestRepo _repo;
    private readonly INotificationRepo? _notificationRepo;
    private readonly INotificationRealtimePublisher? _realtimePublisher;
    private readonly ILogger<StaffEmployeeRequestService>? _logger;

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

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public StaffEmployeeRequestService(
        IStaffEmployeeRequestRepo repo,
        INotificationRepo? notificationRepo = null,
        INotificationRealtimePublisher? realtimePublisher = null,
        ILogger<StaffEmployeeRequestService>? logger = null)
    {
        _repo = repo;
        _notificationRepo = notificationRepo;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task<PagedResponse<StaffEmployeeRequestListItemResponse>> GetPagedAsync(
        Guid staffId,
        StaffEmployeeRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateStaffId(staffId);

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
            staffId,
            query.Reason,
            normalizedRequestType,
            normalizedStatus,
            fromUtc,
            toUtc,
            query.PageNumber,
            query.PageSize,
            cancellationToken,
            query.SortByUpdatedAt);

        var responseItems = items.Select(r =>
        {
            StaffRequestShiftSummaryResponse? shiftSummary = null;
            if (r.RequestType == "LEAVE" && r.LeaveRequestAssignments != null && r.LeaveRequestAssignments.Count > 0)
            {
                var firstLeave = r.LeaveRequestAssignments
                    .Where(l => l.Assignment?.Shift != null)
                    .OrderBy(l => l.Assignment.Shift.ScheduledStartAt)
                    .FirstOrDefault();

                if (firstLeave != null)
                {
                    shiftSummary = new StaffRequestShiftSummaryResponse
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
                shiftSummary = new StaffRequestShiftSummaryResponse
                {
                    TotalShifts = 1,
                    PrimaryShiftName = shift?.ShiftNameSnapshot,
                    PrimaryScheduledStartAt = shift != null ? ToUtc(shift.ScheduledStartAt) : null,
                    PrimaryScheduledEndAt = shift != null ? ToUtc(shift.ScheduledEndAt) : null,
                    RequestedTime = ToUtc(tard.RequestedTime)
                };
            }

            return new StaffEmployeeRequestListItemResponse
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                BranchId = r.BranchId,
                BranchName = r.Branch.BranchName,
                BranchCode = r.Branch.BranchCode,
                SalaryIncreaseDetail = r.SalaryIncreaseRequestDetail == null ? null : new StaffSalaryIncreaseDetailResponse { CurrentContractId = r.SalaryIncreaseRequestDetail.CurrentContractId, ContractNumber = r.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber, CurrentHourlyRate = r.SalaryIncreaseRequestDetail.CurrentHourlyRate, ProposedHourlyRate = r.SalaryIncreaseRequestDetail.ProposedHourlyRate },
                RequestType = r.RequestType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = ToUtc(r.CreatedAt),
                UpdatedAt = ToUtc(r.UpdatedAt),
                ReviewedBy = r.ReviewedBy,
                ReviewerFullName = r.ReviewedByNavigation?.FullName,
                ReviewedAt = ToUtc(r.ReviewedAt),
                ReviewNote = r.ReviewNote,
                ShiftSummary = shiftSummary
            };
        }).ToList();

        return new PagedResponse<StaffEmployeeRequestListItemResponse>
        {
            Items = responseItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalItems = totalCount
        };
    }

    public async Task<StaffEmployeeRequestDetailResponse> GetByIdAsync(
        Guid staffId,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        ValidateStaffId(staffId);

        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var request = await _repo.GetByIdAsync(requestId, staffId, cancellationToken);
        if (request == null)
        {
            throw new KeyNotFoundException("Request not found.");
        }

        return MapToDetailResponse(request);
    }

    public async Task<IReadOnlyList<StaffEligibleShiftResponse>> GetEligibleShiftsAsync(
        Guid staffId,
        string requestType,
        CancellationToken cancellationToken = default)
    {
        ValidateStaffId(staffId);

        if (string.IsNullOrWhiteSpace(requestType))
        {
            throw new ArgumentException("Request type is required.");
        }

        var normalizedType = requestType.Trim().ToUpperInvariant();
        if (normalizedType is not ("LEAVE" or "LATE_ARRIVAL" or "EARLY_LEAVE"))
        {
            throw new ArgumentException($"Eligible shifts only support LEAVE, LATE_ARRIVAL, EARLY_LEAVE; salary requests do not select shifts.");
        }

        var branchId = await GetActiveBranchIdAsync(staffId, cancellationToken);
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var assignments = await _repo.GetEligibleShiftsAsync(staffId, branchId, normalizedType, todayVn, cancellationToken);

        return assignments.Select(a => new StaffEligibleShiftResponse
        {
            AssignmentId = a.AssignmentId,
            ShiftId = a.Shift.ShiftId,
            ShiftName = a.Shift.ShiftNameSnapshot,
            ShiftTemplateName = a.Shift.ShiftTemplate?.ShiftName,
            BranchId = a.Shift.ScheduleWeek.BranchId,
            BranchName = a.Shift.ScheduleWeek.Branch.BranchName,
            ScheduledStartAt = ToUtc(a.Shift.ScheduledStartAt),
            ScheduledEndAt = ToUtc(a.Shift.ScheduledEndAt)
        }).ToList();
    }

    public async Task<StaffEmployeeRequestDetailResponse> SubmitLeaveRequestAsync(
        Guid staffId,
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateStaffId(staffId);

        if (request.AssignmentIds == null || request.AssignmentIds.Count == 0)
        {
            throw new ArgumentException("At least one shift assignment is required.");
        }

        if (request.AssignmentIds.Any(id => id <= 0))
        {
            throw new ArgumentException("All assignment IDs must be positive integers.");
        }

        if (request.AssignmentIds.Distinct().Count() != request.AssignmentIds.Count)
        {
            throw new ArgumentException("Duplicate assignment IDs are not allowed.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required and cannot be empty.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new ArgumentException("Reason must not exceed 500 characters.");
        }

        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        var branchId = staff.BranchId!.Value;
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var created = await _repo.SubmitLeaveRequestAsync(
            staffId,
            branchId,
            request.AssignmentIds,
            request.Reason,
            todayVn,
            cancellationToken);

        await SendStaffRequestNotificationToManagerAsync(staff, created, "LEAVE",
            $"Nhân viên {staff.FullName} đã gửi yêu cầu xin nghỉ phép cho {request.AssignmentIds.Count} ca làm việc. Lý do: {request.Reason.Trim()}");

        var detail = await _repo.GetByIdAsync(created.RequestId, staffId, cancellationToken);
        return MapToDetailResponse(detail ?? created);
    }

    public async Task<StaffEmployeeRequestDetailResponse> SubmitLateArrivalRequestAsync(
        Guid staffId,
        SubmitLateArrivalRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateStaffId(staffId);

        if (request.AssignmentId <= 0)
        {
            throw new ArgumentException("Assignment ID must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required and cannot be empty.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new ArgumentException("Reason must not exceed 500 characters.");
        }

        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        var branchId = staff.BranchId!.Value;
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var requestedTimeUtc = request.RequestedTime.UtcDateTime;

        var created = await _repo.SubmitLateArrivalRequestAsync(
            staffId,
            branchId,
            request.AssignmentId,
            requestedTimeUtc,
            request.Reason,
            todayVn,
            cancellationToken);

        await SendStaffRequestNotificationToManagerAsync(staff, created, "LATE_ARRIVAL",
            $"Nhân viên {staff.FullName} đã gửi yêu cầu xin đi muộn vào lúc {requestedTimeUtc.AddHours(7):HH:mm dd/MM/yyyy}. Lý do: {request.Reason.Trim()}");

        var detail = await _repo.GetByIdAsync(created.RequestId, staffId, cancellationToken);
        return MapToDetailResponse(detail ?? created);
    }

    public async Task<StaffEmployeeRequestDetailResponse> SubmitEarlyLeaveRequestAsync(
        Guid staffId,
        SubmitEarlyLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateStaffId(staffId);

        if (request.AssignmentId <= 0)
        {
            throw new ArgumentException("Assignment ID must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required and cannot be empty.");
        }

        if (request.Reason.Trim().Length > 500)
        {
            throw new ArgumentException("Reason must not exceed 500 characters.");
        }

        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        var branchId = staff.BranchId!.Value;
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var requestedTimeUtc = request.RequestedTime.UtcDateTime;

        var created = await _repo.SubmitEarlyLeaveRequestAsync(
            staffId,
            branchId,
            request.AssignmentId,
            requestedTimeUtc,
            request.Reason,
            todayVn,
            cancellationToken);

        await SendStaffRequestNotificationToManagerAsync(staff, created, "EARLY_LEAVE",
            $"Nhân viên {staff.FullName} đã gửi yêu cầu xin về sớm vào lúc {requestedTimeUtc.AddHours(7):HH:mm dd/MM/yyyy}. Lý do: {request.Reason.Trim()}");

        var detail = await _repo.GetByIdAsync(created.RequestId, staffId, cancellationToken);
        return MapToDetailResponse(detail ?? created);
    }

    public async Task<StaffEmployeeRequestDetailResponse> SubmitSalaryIncreaseRequestAsync(
        Guid staffId, SubmitSalaryIncreaseRequestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); ValidateStaffId(staffId);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw new ArgumentException("Cần lý do xin tăng lương, tối đa 500 ký tự.");
        if (request.ProposedHourlyRate <= 0 || request.ProposedHourlyRate > 9999999999999999.99m
            || request.ProposedHourlyRate != decimal.Truncate(request.ProposedHourlyRate * 100) / 100)
            throw new ArgumentException("Mức đề xuất phải dương và phù hợp NUMERIC(18,2); không tự làm tròn.");
        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        var branchId = staff.BranchId!.Value;
        var created = await _repo.SubmitSalaryIncreaseRequestAsync(staffId, branchId, request.ProposedHourlyRate,
            request.Reason.Trim(), DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)), cancellationToken);

        await SendStaffRequestNotificationToManagerAsync(staff, created, "SALARY_INCREASE",
            $"Nhân viên {staff.FullName} đã gửi yêu cầu xin tăng lương với mức lương đề xuất {request.ProposedHourlyRate:N0} đ/giờ. Lý do: {request.Reason.Trim()}");

        return MapToDetailResponse(await _repo.GetByIdAsync(created.RequestId, staffId, cancellationToken) ?? created);
    }

    public async Task<StaffEmployeeRequestDetailResponse> CancelRequestAsync(
        Guid staffId,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        ValidateStaffId(staffId);

        if (requestId <= 0)
        {
            throw new ArgumentException("Request ID must be a positive integer.");
        }

        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        var cancelled = await _repo.CancelRequestAsync(requestId, staffId, cancellationToken);

        await SendStaffCancelNotificationToManagerAsync(staff, cancelled);

        var detail = await _repo.GetByIdAsync(cancelled.RequestId, staffId, cancellationToken);
        return MapToDetailResponse(detail ?? cancelled);
    }

    public async Task<int> CancelOverduePendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var cancelledList = await _repo.CancelOverduePendingRequestsAsync(nowUtc, cancellationToken);
        if (cancelledList.Count == 0) return 0;

        foreach (var r in cancelledList)
        {
            try
            {
                if (_notificationRepo != null)
                {
                    var typeVietnamese = TranslateRequestType(r.RequestType);
                    var notifTitle = $"Yêu cầu #{r.RequestId} đã tự động hủy do quá hạn";
                    var notifContent = $"Yêu cầu {typeVietnamese} #{r.RequestId} của bạn đã bị hệ thống tự động hủy do ca làm việc đã kết thúc mà chưa được quản lý phê duyệt.";

                    var notification = new NotificationEntity
                    {
                        Title = notifTitle,
                        Content = notifContent,
                        NotificationType = "EmployeeRequest",
                        CreatedBy = r.UserId,
                        CreatedAt = nowUtc,
                        UpdatedAt = nowUtc,
                        IsDeleted = false,
                        TargetType = "Custom",
                        TargetBranchId = null,
                        TargetRoles = new List<string> { "STAFF" },
                        Channels = new List<string> { "InApp" }
                    };

                    await _notificationRepo.CreateAsync(notification, new[] { r.UserId }, true);

                    if (_realtimePublisher != null)
                    {
                        var realtime = new RealtimeNotificationResponse
                        {
                            NotificationId = notification.NotificationId,
                            Title = notification.Title,
                            Message = notification.Content,
                            CreatedAt = notification.CreatedAt
                        };

                        try
                        {
                            await _realtimePublisher.PublishAsync(r.UserId, realtime);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to publish realtime overdue cancel notification to staff {UserId}", r.UserId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send notification for auto-cancelled request #{RequestId}", r.RequestId);
            }
        }

        return cancelledList.Count;
    }

    private async Task SendStaffRequestNotificationToManagerAsync(
        User staff,
        EmployeeRequest createdRequest,
        string requestType,
        string detailMessage)
    {
        if (_notificationRepo == null) return;

        try
        {
            var branchManagers = await _notificationRepo.GetRecipientsAsync(new[] { "MANAGER" }, null, createdRequest.BranchId);
            if (branchManagers == null || branchManagers.Count == 0)
            {
                _logger?.LogInformation("No active managers found for branch {BranchId} to send request notification", createdRequest.BranchId);
                return;
            }

            var typeVietnamese = TranslateRequestType(requestType);
            var title = $"Yêu cầu {typeVietnamese} mới từ {staff.FullName}";
            var now = DateTime.UtcNow;

            var notification = new NotificationEntity
            {
                Title = title,
                Content = detailMessage,
                NotificationType = "EmployeeRequest",
                CreatedBy = staff.UserId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Branch",
                TargetBranchId = createdRequest.BranchId,
                TargetRoles = new(),
                Channels = new List<string> { "InApp" }
            };

            var recipientUserIds = branchManagers.Select(m => m.UserId).Distinct().ToList();
            await _notificationRepo.CreateAsync(notification, recipientUserIds, true);

            if (_realtimePublisher != null)
            {
                var realtime = new RealtimeNotificationResponse
                {
                    NotificationId = notification.NotificationId,
                    Title = notification.Title,
                    Message = notification.Content,
                    CreatedAt = notification.CreatedAt
                };

                foreach (var managerId in recipientUserIds)
                {
                    try
                    {
                        await _realtimePublisher.PublishAsync(managerId, realtime);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to publish realtime notification to manager {UserId} for request {RequestId}", managerId, createdRequest.RequestId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create/send request notification to managers for request {RequestId}", createdRequest.RequestId);
        }
    }

    private async Task SendStaffCancelNotificationToManagerAsync(
        User staff,
        EmployeeRequest cancelledRequest)
    {
        if (_notificationRepo == null) return;

        try
        {
            var branchManagers = await _notificationRepo.GetRecipientsAsync(new[] { "MANAGER" }, null, cancelledRequest.BranchId);
            if (branchManagers == null || branchManagers.Count == 0) return;

            var typeVietnamese = TranslateRequestType(cancelledRequest.RequestType);
            var title = $"Nhân viên đã hủy yêu cầu #{cancelledRequest.RequestId}";
            var content = $"Nhân viên {staff.FullName} đã hủy yêu cầu {typeVietnamese} #{cancelledRequest.RequestId}.";
            var now = DateTime.UtcNow;

            var notification = new NotificationEntity
            {
                Title = title,
                Content = content,
                NotificationType = "EmployeeRequest",
                CreatedBy = staff.UserId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = "Branch",
                TargetBranchId = cancelledRequest.BranchId,
                TargetRoles = new(),
                Channels = new List<string> { "InApp" }
            };

            var recipientUserIds = branchManagers.Select(m => m.UserId).Distinct().ToList();
            await _notificationRepo.CreateAsync(notification, recipientUserIds, true);

            if (_realtimePublisher != null)
            {
                var realtime = new RealtimeNotificationResponse
                {
                    NotificationId = notification.NotificationId,
                    Title = notification.Title,
                    Message = notification.Content,
                    CreatedAt = notification.CreatedAt
                };

                foreach (var managerId in recipientUserIds)
                {
                    try
                    {
                        await _realtimePublisher.PublishAsync(managerId, realtime);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to publish realtime cancel notification to manager {UserId} for request {RequestId}", managerId, cancelledRequest.RequestId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create/send cancel notification to managers for request {RequestId}", cancelledRequest.RequestId);
        }
    }

    private static string TranslateRequestType(string requestType) => requestType?.ToUpperInvariant() switch
    {
        "LEAVE" => "xin nghỉ phép",
        "LATE_ARRIVAL" => "xin đi muộn",
        "EARLY_LEAVE" => "xin về sớm",
        "SALARY_INCREASE" => "xin tăng lương",
        _ => requestType ?? "khác"
    };

    private static void ValidateStaffId(Guid staffId)
    {
        if (staffId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid authenticated user ID is required.");
        }
    }

    private async Task<User> GetActiveStaffOrThrowAsync(Guid staffId, CancellationToken cancellationToken)
    {
        var staff = await _repo.GetActiveStaffAsync(staffId, cancellationToken);
        if (staff == null)
        {
            throw new UnauthorizedAccessException("Staff account not found or deactivated.");
        }

        if (staff.Role?.RoleName != "STAFF" || staff.Role?.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("User does not have an active STAFF role.");
        }

        if (!staff.BranchId.HasValue || staff.Branch == null || staff.Branch.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Staff is not assigned to an active branch.");
        }

        return staff;
    }

    private async Task<int> GetActiveBranchIdAsync(Guid staffId, CancellationToken cancellationToken)
    {
        var staff = await GetActiveStaffOrThrowAsync(staffId, cancellationToken);
        return staff.BranchId!.Value;
    }

    private static StaffEmployeeRequestDetailResponse MapToDetailResponse(EmployeeRequest request)
    {
        var response = new StaffEmployeeRequestDetailResponse
        {
            RequestId = request.RequestId,
            UserId = request.UserId,
            BranchId = request.BranchId,
            BranchName = request.Branch?.BranchName ?? string.Empty,
            BranchCode = request.Branch?.BranchCode ?? string.Empty,
            SalaryIncreaseDetail = request.SalaryIncreaseRequestDetail == null ? null : new StaffSalaryIncreaseDetailResponse { CurrentContractId = request.SalaryIncreaseRequestDetail.CurrentContractId, ContractNumber = request.SalaryIncreaseRequestDetail.CurrentContract?.ContractNumber, CurrentHourlyRate = request.SalaryIncreaseRequestDetail.CurrentHourlyRate, ProposedHourlyRate = request.SalaryIncreaseRequestDetail.ProposedHourlyRate },
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
                ? new StaffAppliedRequestLimitRuleResponse
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
                .Select(lra => new StaffLeaveRequestAssignmentResponse
                {
                    LeaveRequestAssignmentId = lra.LeaveRequestAssignmentId,
                    AssignmentId = lra.AssignmentId,
                    AssignmentStatus = lra.Assignment?.Status ?? string.Empty,
                    ShiftId = lra.Assignment?.Shift?.ShiftId ?? 0,
                    ShiftName = lra.Assignment?.Shift?.ShiftNameSnapshot ?? string.Empty,
                    ShiftTemplateName = lra.Assignment?.Shift?.ShiftTemplate?.ShiftName,
                    ScheduledStartAt = ToUtc(lra.Assignment?.Shift?.ScheduledStartAt ?? default),
                    ScheduledEndAt = ToUtc(lra.Assignment?.Shift?.ScheduledEndAt ?? default),
                    BranchName = lra.Assignment?.Shift?.ScheduleWeek?.Branch?.BranchName
                })
                .OrderBy(a => a.ScheduledStartAt)
                .ThenBy(a => a.AssignmentId)
                .ToList();
        }
        else if (request.TimeAdjustmentRequestDetail != null)
        {
            var tard = request.TimeAdjustmentRequestDetail;
            response.TimeAdjustmentDetail = new StaffTimeAdjustmentDetailResponse
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
