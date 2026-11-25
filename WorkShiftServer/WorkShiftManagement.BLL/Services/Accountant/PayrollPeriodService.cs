using WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Request;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollPeriod.Response;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Policies.Payroll;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class PayrollPeriodService(IPayrollPeriodRepo repo) : IPayrollPeriodService
{
    private static readonly string[] Statuses =
    [
        "DRAFT",
        "OPEN",
        "CALCULATED",
        "SUBMITTED",
        "REVISION_REQUIRED",
        "APPROVED",
        "PAID",
        "CLOSED"
    ];

    private async Task<User> ActorAsync(
        Guid id,
        bool allowAdmin,
        CancellationToken ct)
    {
        var user = await repo.GetActorAsync(id, ct);

        if (user == null
            || user.Status != "ACTIVE"
            || user.DeletedAt != null
            || user.Role.Status != "ACTIVE"
            || (user.Role.RoleName != "ACCOUNTANT"
                && !(allowAdmin && user.Role.RoleName == "ADMIN"))
            || (user.Role.RoleName == "ACCOUNTANT"
                && user.BranchId is null or <= 0))
        {
            throw new UnauthorizedAccessException(
                "An active Accountant assigned to a branch is required.");
        }

        return user;
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be greater than zero.");
    }

    private static string? ValidateQuery(PayrollPeriodListRequest query)
    {
        if (query.PageNumber < 1
            || query.PageSize is < 1 or > 100
            || (long)(query.PageNumber - 1) * query.PageSize > int.MaxValue)
        {
            throw new ArgumentException(
                "PageNumber must be positive and in range; PageSize must be between 1 and 100.");
        }

        if (query.Search?.Trim().Length > 200)
            throw new ArgumentException("Search cannot exceed 200 characters.");

        if (query.StartDateFrom > query.StartDateTo)
            throw new ArgumentException("StartDateFrom must not exceed StartDateTo.");

        var status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToUpperInvariant();

        if (status != null && !Statuses.Contains(status))
        {
            throw new ArgumentException(
                $"Status must be one of: {string.Join(", ", Statuses)}.");
        }

        return status;
    }

    private static string ValidateName(SavePayrollPeriodRequest request)
    {
        ValidateId(request.ScheduleWeekId);

        var name = request.PeriodName?.Trim();

        if (string.IsNullOrEmpty(name) || name.Length > 200)
        {
            throw new ArgumentException(
                "PeriodName must contain 1 to 200 characters.");
        }

        return name;
    }

    private async Task<PayrollPeriod> PeriodAsync(
        int id,
        int? branch,
        bool tracking,
        CancellationToken ct)
    {
        ValidateId(id);

        return await repo.GetByIdAsync(id, branch, tracking, ct)
            ?? throw new KeyNotFoundException(
                "Payroll period was not found in your permitted scope.");
    }

    private async Task RequireEditableAsync(
        PayrollPeriod period,
        CancellationToken ct)
    {
        if (period.Status != "DRAFT")
        {
            throw new InvalidOperationException(
                "Only DRAFT periods can be updated or deleted.");
        }

        if (await repo.HasDependenciesAsync(period.PayrollPeriodId, ct))
        {
            throw new InvalidOperationException(
                "The draft period already has timesheets, payrolls or employee adjustments.");
        }
    }

    private async Task<WorkScheduleWeek> WeekAsync(
        int id,
        int branch,
        int? excluding,
        CancellationToken ct)
    {
        var week = await repo.GetWeekAsync(id, branch, true, ct)
            ?? throw new KeyNotFoundException(
                "Work schedule week was not found in your branch.");

        if (week.Status == "CANCELLED" || week.Branch.Status != "ACTIVE")
        {
            throw new InvalidOperationException(
                "The schedule week or branch is unavailable.");
        }

        if (week.WeekStartDate.DayOfWeek != DayOfWeek.Monday
            || week.WeekEndDate != week.WeekStartDate.AddDays(6))
        {
            throw new InvalidOperationException(
                "Schedule week must run from Monday through Sunday.");
        }

        if (week.WeekStartDate > DateOnly.MaxValue.AddDays(-10))
        {
            throw new ArgumentException(
                "The schedule week is outside the supported date range.");
        }

        if (await repo.WeekHasPeriodAsync(id, excluding, ct))
        {
            throw new InvalidOperationException(
                "This schedule week already has a payroll period, including drafts.");
        }

        return week;
    }

    private static void ApplyWeek(
        PayrollPeriod period,
        WorkScheduleWeek week)
    {
        period.ScheduleWeekId = week.ScheduleWeekId;
        period.BranchId = week.BranchId;
        period.BranchNameSnapshot = week.Branch.BranchName;
        period.StartDate = week.WeekStartDate;
        period.EndDate = week.WeekEndDate;
        period.PeriodCode = $"PAY-{week.BranchId}-{week.WeekStartDate:yyyyMMdd}";

        // 00:00 Thursday of the next week in Vietnam (UTC+7).
        period.ApprovalDeadlineAt = new DateTimeOffset(
            week.WeekStartDate
                .AddDays(10)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(7))
            .UtcDateTime;
    }

    private Task<PayrollPeriodManagementResponse> MapAsync(
        PayrollPeriod p,
        bool includePayrolls,
        CancellationToken ct) => MapAsync(p, includePayrolls, null, ct);

    private async Task<PayrollPeriodManagementResponse> MapAsync(
        PayrollPeriod p,
        bool includePayrolls,
        int? expectedEmployeeCount,
        CancellationToken ct)
    {
        var current = PayrollVersionPolicy.Current(p.Payrolls, p.Status);

        var editable =
            p.Status == "DRAFT"
            && p.Timesheets.Count == 0
            && p.Payrolls.Count == 0
            && p.EmployeeRewardsPenalties.Count == 0;

        return new()
        {
            PayrollPeriodId = p.PayrollPeriodId,
            ScheduleWeekId = p.ScheduleWeekId,
            PeriodCode = p.PeriodCode,
            PeriodName = p.PeriodName,
            BranchId = p.BranchId,
            BranchName = p.BranchNameSnapshot,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            CreatedBy = p.CreatedBy,
            CreatedAt = p.CreatedAt,
            ApprovalDeadlineAt = p.ApprovalDeadlineAt,
            PublishedBy = p.PublishedBy, PublishedAt = p.PublishedAt, InitialFeedbackDeadlineAt = p.InitialFeedbackDeadlineAt,
            ReviewedTimesheetCount = p.Timesheets.Count(t => t.Status == "REVIEWED"),
            PaidAt = p.PaidAt,
            ClosedAt = p.ClosedAt,

            ExpectedEmployeeCount =
                expectedEmployeeCount ?? await repo.ExpectedEmployeeCountAsync(p, ct),

            LockedTimesheetCount = p.Timesheets
                .Where(t => t.Status == "LOCKED")
                .Select(t => t.UserId)
                .Distinct()
                .Count(),

            TotalPayrolls = current.Count,

            RejectedPayrollCount =
                current.Count(w => w.Status == "REJECTED"),

            PendingCorrectionCount = p.Payrolls.SelectMany(w => w.PayrollCorrectionRequestPayrolls)
                .Count(r => r.Status is "OPEN" or "RESPONDED"),

            TotalNetSalary = current.Sum(w => w.NetSalary),

            CanUpdate = editable,
            CanDelete = editable,

            CanOpen =
                p.Status == "DRAFT"
                && p.Payrolls.Count == 0
                && !string.IsNullOrWhiteSpace(p.PeriodName)
                && p.ScheduleWeekId != null
                && p.ScheduleWeek != null
                && p.StartDate == p.ScheduleWeek.WeekStartDate
                && p.EndDate == p.ScheduleWeek.WeekEndDate
                && p.ScheduleWeek.Status != "CANCELLED"
                && p.ScheduleWeek.Branch.Status == "ACTIVE",

            Payrolls = includePayrolls
                ? current
                    .OrderBy(w => w.EmployeeFullNameSnapshot)
                    .ThenBy(w => w.PayrollId)
                    .Select(w => new PayrollResponse
                    {
                        PayrollId = w.PayrollId,
                        UserId = w.UserId,
                        EmployeeFullName = w.EmployeeFullNameSnapshot,
                        EmployeePosition = w.EmployeePositionSnapshot,
                        HourlyRate = w.HourlyRateSnapshot,
                        TotalWorkHours = w.TotalWorkHours,
                        WorkPay = w.WorkPay,
                        TotalReward = w.TotalReward,
                        TotalPenalty = w.TotalPenalty,
                        AttendanceDeduction = w.AttendanceDeduction,
                        GrossSalary = w.GrossSalary,
                        TotalDeduction = w.TotalDeduction,
                        NetSalary = w.NetSalary,
                        Status = w.Status,
                        VersionNo = w.VersionNo,
                        PreviousPayrollId = w.PreviousPayrollId,
                        CorrectionReason = w.CorrectionReason,
                        CarryForwardDeduction = w.CarryForwardDeduction,
                        DeferredDeductionAmount = w.DeferredDeductionAmount,

                        Details = w.PayrollDetails
                            .OrderBy(d => d.DisplayOrder)
                            .ThenBy(d => d.PayrollDetailId)
                            .Select(d => new PayrollDetailResponse
                            {
                                PayrollDetailId = d.PayrollDetailId,
                                Type = d.Type,
                                ItemCode = d.ItemCode,
                                Description = d.Description,
                                Multiplier = d.Multiplier,
                                WorkFrom = d.WorkFrom,
                                WorkTo = d.WorkTo,
                                Quantity = d.Quantity,
                                Rate = d.Rate,
                                Amount = d.Amount,
                                AppliedAmount = d.AppliedAmount, SalaryHistoryId = d.SalaryHistoryId,
                                RewardPenaltyId = d.RewardPenaltyId, AttendancePenaltyId = d.AttendancePenaltyId,
                                CarryForwardId = d.CarryForwardId, PublicHolidayId = d.PublicHolidayId,
                                HolidayPayRuleId = d.HolidayPayRuleId,
                                DisplayOrder = d.DisplayOrder,
                                Note = d.Note
                            })
                            .ToList()
                    })
                    .ToList()
                : []
        };
    }

    public async Task<PagedResponse<PayrollPeriodManagementResponse>> GetAllAsync(
        Guid actor,
        PayrollPeriodListRequest query,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);
        var status = ValidateQuery(query);

        var (items, count) = await repo.GetPagedAsync(
            user.BranchId!.Value,
            query.Search,
            status,
            query.StartDateFrom,
            query.StartDateTo,
            query.PageNumber,
            query.PageSize,
            ct);

        var result = new PagedResponse<PayrollPeriodManagementResponse>
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalItems = count
        };

        var expectedCounts = await repo.GetExpectedEmployeeCountsAsync(items, ct);

        foreach (var item in items)
        {
            result.Items.Add(
                await MapAsync(item, false, expectedCounts.GetValueOrDefault(item.PayrollPeriodId, 0), ct));
        }

        return result;
    }

    public async Task<PayrollPeriodManagementResponse> GetByIdAsync(
        Guid actor,
        int id,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);

        return await MapAsync(
            await PeriodAsync(id, user.BranchId, false, ct),
            true,
            null,
            ct);
    }

    public async Task<PagedResponse<AvailableScheduleWeekResponse>> GetAvailableWeeksAsync(
        Guid actor,
        PayrollPeriodListRequest query,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);

        ValidateQuery(query);

        if (!string.IsNullOrWhiteSpace(query.Search)
            || !string.IsNullOrWhiteSpace(query.Status))
        {
            throw new ArgumentException(
                "Available weeks support StartDateFrom, StartDateTo and pagination only.");
        }

        var (items, count) = await repo.GetAvailableWeeksAsync(
            user.BranchId!.Value,
            query.StartDateFrom,
            query.StartDateTo,
            query.PageNumber,
            query.PageSize,
            ct);

        return new()
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalItems = count,

            Items = items
                .Select(w => new AvailableScheduleWeekResponse
                {
                    ScheduleWeekId = w.ScheduleWeekId,
                    BranchId = w.BranchId,
                    WeekStartDate = w.WeekStartDate,
                    WeekEndDate = w.WeekEndDate,
                    Status = w.Status
                })
                .ToList()
        };
    }

    public async Task<PayrollPeriodManagementResponse> CreateAsync(
        Guid actor,
        SavePayrollPeriodRequest request,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);
        var name = ValidateName(request);

        await using var tx = await repo.BeginWriteAsync(ct);

        var week = await WeekAsync(
            request.ScheduleWeekId,
            user.BranchId!.Value,
            null,
            ct);

        var p = new PayrollPeriod
        {
            PeriodName = name,
            Status = "DRAFT",
            CreatedBy = actor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ScheduleWeek = week
        };

        ApplyWeek(p, week);

        repo.Add(p);

        await repo.SaveAsync(ct);

        repo.Audit(
            actor,
            "CREATE_PAYROLL_PERIOD",
            p,
            null);

        await repo.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return await MapAsync(p, false, ct);
    }

    public async Task<PayrollPeriodManagementResponse> UpdateAsync(
        Guid actor,
        int id,
        UpdatePayrollPeriodRequest request,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);

        if (request.ScheduleWeekId == null
            && request.PeriodName == null)
        {
            throw new ArgumentException(
                "Provide ScheduleWeekId or PeriodName to update.");
        }

        await using var tx = await repo.BeginWriteAsync(ct);

        var p = await PeriodAsync(
            id,
            user.BranchId,
            true,
            ct);

        await RequireEditableAsync(p, ct);

        var before = new
        {
            p.PeriodName,
            p.ScheduleWeekId,
            p.StartDate,
            p.EndDate,
            p.PeriodCode
        };

        var name = ValidateName(
            new SavePayrollPeriodRequest
            {
                ScheduleWeekId =
                    request.ScheduleWeekId
                    ?? p.ScheduleWeekId
                    ?? 0,

                PeriodName =
                    request.PeriodName
                    ?? p.PeriodName
            });

        var week = await WeekAsync(
            request.ScheduleWeekId
                ?? p.ScheduleWeekId
                ?? 0,
            user.BranchId!.Value,
            id,
            ct);

        ApplyWeek(p, week);

        p.ScheduleWeek = week;
        p.PeriodName = name;
        p.UpdatedAt = DateTime.UtcNow;

        repo.Audit(
            actor,
            "UPDATE_PAYROLL_PERIOD",
            p,
            before);

        await repo.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return await MapAsync(p, false, ct);
    }

    public async Task DeleteAsync(
        Guid actor,
        int id,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);

        await using var tx = await repo.BeginWriteAsync(ct);

        var p = await PeriodAsync(
            id,
            user.BranchId,
            true,
            ct);

        await RequireEditableAsync(p, ct);

        repo.Audit(
            actor,
            "DELETE_PAYROLL_PERIOD",
            p,
            new
            {
                p.PeriodCode,
                p.PeriodName,
                p.ScheduleWeekId,
                p.Status
            });

        repo.Remove(p);

        await repo.SaveAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<PayrollPeriodManagementResponse> OpenAsync(
        Guid actor,
        int id,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, false, ct);

        await using var tx = await repo.BeginWriteAsync(ct);

        var p = await PeriodAsync(
            id,
            user.BranchId,
            true,
            ct);

        if (p.Status != "DRAFT")
        {
            throw new InvalidOperationException(
                "Only a DRAFT payroll period can be opened.");
        }

        // Timesheets and approved adjustments are inputs to calculation, not a reason to block opening.
        // The period is locked and its Payrolls are loaded by PeriodAsync above.
        if (p.Payrolls.Count != 0)
        {
            throw new InvalidOperationException(
                "A draft with existing Payroll records cannot be opened as a new period.");
        }

        if (p.ScheduleWeekId == null
            || string.IsNullOrWhiteSpace(p.PeriodName))
        {
            throw new InvalidOperationException(
                "Schedule week and name are required.");
        }

        var week = await WeekAsync(
            p.ScheduleWeekId.Value,
            user.BranchId!.Value,
            id,
            ct);

        if (p.StartDate != week.WeekStartDate
            || p.EndDate != week.WeekEndDate)
        {
            throw new InvalidOperationException(
                "Period dates do not match its schedule week.");
        }

        var before = new
        {
            p.Status
        };

        p.Status = "OPEN";
        p.PublishedBy = actor; p.PublishedAt = DateTime.UtcNow;
        p.UpdatedAt = DateTime.UtcNow;

        repo.Audit(
            actor,
            "OPEN_PAYROLL_PERIOD",
            p,
            before);

        await repo.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return await MapAsync(p, false, ct);
    }

    public async Task<PayrollPeriodReportResponse> ExportAsync(
        Guid actor,
        int id,
        CancellationToken ct)
    {
        var user = await ActorAsync(actor, true, ct);

        var p = await PeriodAsync(
            id,
            user.Role.RoleName == "ADMIN"
                ? null
                : user.BranchId,
            false,
            ct);

        if (p.Status is not ("APPROVED" or "PAID" or "CLOSED"))
        {
            throw new InvalidOperationException(
                "Excel export is available only for APPROVED, PAID, or CLOSED periods.");
        }

        var current = PayrollVersionPolicy.Current(
            p.Payrolls,
            p.Status);

        if (current.Count == 0)
        {
            throw new InvalidOperationException(
                "The period does not contain any effective payroll records.");
        }

        if (p.Status is "PAID" or "CLOSED" && current.Any(w => w.Status != "PAID"))
        {
            throw new InvalidOperationException(
                "The settled period must contain paid current payrolls.");
        }

        if (p.Status == "APPROVED" && current.Any(w => w.Status is not ("APPROVED" or "PAID")))
        {
            throw new InvalidOperationException(
                "The approved period must contain approved current payrolls.");
        }

        var cleanBranch = new string(p.BranchNameSnapshot.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()).Replace(" ", "_");
        if (string.IsNullOrWhiteSpace(cleanBranch)) cleanBranch = "Branch";
        var cleanCode = new string(p.PeriodCode.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()).Replace(" ", "");
        var fileName = $"WorkShift_Payroll_{cleanBranch}_{cleanCode}_{p.StartDate:yyyyMMdd}_{p.EndDate:yyyyMMdd}.xlsx";

        return new(
            PayrollPeriodExcelReport.Create(p, current, user.FullName),
            fileName);
    }
}
