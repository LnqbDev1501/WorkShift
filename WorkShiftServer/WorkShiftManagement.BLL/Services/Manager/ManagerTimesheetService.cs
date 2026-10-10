using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ManagerTimesheetService : IManagerTimesheetService
{
    private readonly IManagerTimesheetRepo _timesheetRepo;
    private readonly ILogger<ManagerTimesheetService> _logger;

    public ManagerTimesheetService(
        IManagerTimesheetRepo timesheetRepo,
        ILogger<ManagerTimesheetService> logger)
    {
        _timesheetRepo = timesheetRepo;
        _logger = logger;
    }

    public async Task<ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>> GetWeeklyTimesheetsAsync(
        Guid managerId,
        ManagerTimesheetListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated manager identity is required.");
        }

        ArgumentNullException.ThrowIfNull(query);

        var manager = await _timesheetRepo.GetActiveManagerAsync(managerId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Manager account is inactive or was not found.");

        int branchId = manager.BranchId
            ?? throw new InvalidOperationException("Manager is not currently assigned to any branch.");

        if (query.BranchId.HasValue && query.BranchId.Value != branchId && manager.Role.RoleName != "ADMIN")
        {
            throw new UnauthorizedAccessException("You are only authorized to view timesheets for your assigned branch.");
        }

        int targetBranchId = query.BranchId ?? branchId;

        DateOnly? fromDate = query.FromDate;
        DateOnly? toDate = query.ToDate;

        if (query.Month.HasValue && query.Year.HasValue)
        {
            fromDate = new DateOnly(query.Year.Value, query.Month.Value, 1);
            toDate = fromDate.Value.AddMonths(1).AddDays(-1);
        }

        int pageIndex = Math.Max(1, query.PageIndex);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (weeks, totalCount) = await _timesheetRepo.GetWeeklyTimesheetsAsync(
            targetBranchId,
            query.Search,
            query.ScheduleWeekId,
            fromDate,
            toDate,
            query.Status,
            pageIndex,
            pageSize,
            cancellationToken);

        var availableWeeks = await _timesheetRepo.GetAvailableWeeksForBranchAsync(targetBranchId, cancellationToken);
        var availablePeriods = availableWeeks.Select(w => new ManagerTimesheetPeriodOptionDto
        {
            ScheduleWeekId = w.ScheduleWeekId,
            Period = $"{w.WeekStartDate:dd/MM} - {w.WeekEndDate:dd/MM}",
            WeekStartDate = w.WeekStartDate,
            WeekEndDate = w.WeekEndDate
        }).ToList();

        var items = weeks.Select(MapWeeklySummary).ToList();

        return new ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>
        {
            Items = items,
            AvailablePeriods = availablePeriods,
            TotalItems = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    private static ManagerWeeklyTimesheetSummaryResponse MapWeeklySummary(WorkScheduleWeek week)
    {
        var timesheets = week.PayrollPeriod?.Timesheets.ToList() ?? new List<Timesheet>();
        int totalStaff;
        int confirmedStaff;
        int totalWorkMinutes = 0;
        int totalLateMinutes = 0;
        int totalEarlyLeaveMinutes = 0;
        string status;

        if (timesheets.Count > 0)
        {
            totalStaff = timesheets.Count;
            confirmedStaff = timesheets.Count(t => string.Equals(t.StaffReviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase));
            totalWorkMinutes = timesheets.Sum(t => t.TotalWorkMinutes);
            totalLateMinutes = timesheets.Sum(t => t.TotalLateMinutes);
            totalEarlyLeaveMinutes = timesheets.Sum(t => t.TotalEarlyLeaveMinutes);

            if (timesheets.All(t => string.Equals(t.Status, "LOCKED", StringComparison.OrdinalIgnoreCase)))
            {
                status = "LOCKED";
            }
            else if (week.PayrollPeriod != null && !string.IsNullOrWhiteSpace(week.PayrollPeriod.Status))
            {
                status = week.PayrollPeriod.Status;
            }
            else
            {
                status = "GENERATED";
            }
        }
        else
        {
            // Fallback from active assigned shifts in this week
            totalStaff = week.WorkShifts
                .SelectMany(s => s.ShiftAssignments)
                .Where(a => a.Status != "CANCELLED")
                .Select(a => a.UserId)
                .Distinct()
                .Count();

            confirmedStaff = 0;
            status = week.Status ?? "DRAFT";
        }

        decimal confirmationRate = totalStaff > 0
            ? Math.Round((decimal)confirmedStaff / totalStaff * 100m, 2)
            : 0m;

        decimal totalWorkHours = Math.Round((decimal)totalWorkMinutes / 60m, 2);

        return new ManagerWeeklyTimesheetSummaryResponse
        {
            ScheduleWeekId = week.ScheduleWeekId,
            PayrollPeriodId = week.PayrollPeriod?.PayrollPeriodId,
            Period = $"{week.WeekStartDate:dd/MM} - {week.WeekEndDate:dd/MM}",
            WeekStartDate = week.WeekStartDate,
            WeekEndDate = week.WeekEndDate,
            BranchId = week.BranchId,
            BranchName = week.Branch?.BranchName ?? $"Branch {week.BranchId}",
            Status = status,
            TotalStaff = totalStaff,
            ConfirmedStaff = confirmedStaff,
            StaffReview = $"{confirmedStaff}/{totalStaff} Confirmed",
            ConfirmationRate = confirmationRate,
            TotalWorkHours = totalWorkHours,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes,
            IsLocked = string.Equals(status, "LOCKED", StringComparison.OrdinalIgnoreCase)
        };
    }
}
