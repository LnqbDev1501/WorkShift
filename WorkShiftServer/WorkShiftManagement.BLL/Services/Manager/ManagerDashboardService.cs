using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ManagerDashboardService(IManagerDashboardRepo repo) : IManagerDashboardService
{
    public async Task<ManagerDashboardResponse> GetAsync(Guid managerId, CancellationToken ct = default)
    {
        if (managerId == Guid.Empty) throw new UnauthorizedAccessException("A valid authenticated user ID is required.");
        var manager = await repo.GetManagerAsync(managerId, ct);
        if (manager == null || manager.Status != "ACTIVE" || manager.Role?.RoleName != "MANAGER" || manager.Role.Status != "ACTIVE" ||
            !manager.BranchId.HasValue || manager.Branch?.Status != "ACTIVE")
            throw new UnauthorizedAccessException("An active Manager account assigned to an active branch is required.");

        var branchId = manager.BranchId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var utcStart = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
        var utcEnd = utcStart.AddDays(1);
        // Execute sequentially: these queries share one EF Core DbContext.
        var staffCounts = await repo.Staff(branchId).GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var attendanceCounts = await repo.Attendances(branchId)
            .Where(x => x.ShiftStartAtSnapshot >= utcStart && x.ShiftStartAtSnapshot < utcEnd)
            .GroupBy(x => x.Status)
            .Select(g => new AttendanceStatusResponse { Status = g.Key, Count = g.Count() })
            .OrderBy(x => x.Status).ToListAsync(ct);
        var pendingCounts = await repo.Requests(branchId).Where(x => x.Status == "PENDING")
            .GroupBy(x => x.RequestType)
            .Select(g => new ManagerDashboardRequestCountResponse { RequestType = g.Key, Count = g.Count() })
            .OrderBy(x => x.RequestType).ToListAsync(ct);
        var recentRequests = await repo.Requests(branchId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.RequestId)
            .Take(5).Select(x => new ManagerDashboardRecentRequestResponse
            {
                RequestId = x.RequestId, UserId = x.UserId, FullName = x.User.FullName,
                RequestType = x.RequestType, Status = x.Status, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
        var week = await repo.ScheduleWeeks(branchId)
            .Where(x => x.WeekStartDate <= today && x.WeekEndDate >= today && x.Status != "CANCELLED")
            .OrderByDescending(x => x.WeekStartDate).ThenByDescending(x => x.ScheduleWeekId)
            .Select(x => new ManagerDashboardScheduleWeekResponse
            {
                ScheduleWeekId = x.ScheduleWeekId, WeekStartDate = x.WeekStartDate,
                WeekEndDate = x.WeekEndDate, Status = x.Status, RegistrationDeadline = x.RegistrationDeadline
            }).FirstOrDefaultAsync(ct);

        return new ManagerDashboardResponse
        {
            BranchId = branchId, BranchName = manager.Branch.BranchName, Date = today,
            TotalStaff = staffCounts.Sum(x => x.Count),
            ActiveStaff = staffCounts.FirstOrDefault(x => x.Status == "ACTIVE")?.Count ?? 0,
            InactiveStaff = staffCounts.FirstOrDefault(x => x.Status == "INACTIVE")?.Count ?? 0,
            TodayShifts = await repo.Shifts(branchId)
                .CountAsync(x => x.ScheduledStartAt >= utcStart && x.ScheduledStartAt < utcEnd && x.Status != "CANCELLED" && x.ScheduleWeek.Status != "CANCELLED", ct),
            TodayAssignments = await repo.Assignments(branchId)
                .CountAsync(x => x.Status == "ASSIGNED" && x.Shift.Status != "CANCELLED" && x.Shift.ScheduleWeek.Status != "CANCELLED" &&
                    x.Shift.ScheduledStartAt >= utcStart && x.Shift.ScheduledStartAt < utcEnd, ct),
            TodayAttendance = new AttendanceSummaryResponse { Date = today, Total = attendanceCounts.Sum(x => x.Count), ByStatus = attendanceCounts },
            PendingRequests = pendingCounts.Sum(x => x.Count), PendingRequestsByType = pendingCounts,
            RecentRequests = recentRequests, CurrentScheduleWeek = week
        };
    }
}
