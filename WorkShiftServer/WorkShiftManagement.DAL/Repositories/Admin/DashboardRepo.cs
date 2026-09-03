using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class DashboardRepo : IDashboardRepo
{
    private readonly AppDbContext _context;

    public DashboardRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardData> GetAsync(DateTime todayStartUtc, DateTime tomorrowStartUtc)
    {
        var roleCounts = await _context.Users
            .AsNoTracking()
            .GroupBy(user => user.Role.RoleName.ToUpper())
            .Select(group => new { RoleName = group.Key, Count = group.Count() })
            .ToListAsync();

        var attendanceStatuses = await _context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.ShiftStartAtSnapshot >= todayStartUtc
                && attendance.ShiftStartAtSnapshot < tomorrowStartUtc)
            .GroupBy(attendance => attendance.Status)
            .Select(group => new AttendanceStatusData
            {
                Status = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToListAsync();

        var recentPayrolls = await _context.Payrolls
            .AsNoTracking()
            .OrderByDescending(payroll => payroll.UpdatedAt)
            .ThenByDescending(payroll => payroll.PayrollId)
            .Take(3)
            .Select(payroll => new RecentPayrollData
            {
                PayrollId = payroll.PayrollId,
                EmployeeName = payroll.EmployeeFullNameSnapshot,
                BranchName = payroll.BranchNameSnapshot,
                PeriodName = payroll.PayrollPeriod.PeriodName,
                NetSalary = payroll.NetSalary,
                Status = payroll.Status,
                UpdatedAt = payroll.UpdatedAt
            })
            .ToListAsync();

        var recentRequests = await _context.EmployeeRequests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.RequestId)
            .Take(5)
            .Select(request => new RecentRequestData
            {
                RequestId = request.RequestId,
                EmployeeName = request.User.FullName,
                BranchName = request.Branch.BranchName,
                RequestType = request.RequestType,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            })
            .ToListAsync();

        return new DashboardData
        {
            TotalUsers = roleCounts.Sum(role => role.Count),
            ManagerCount = roleCounts.FirstOrDefault(role => role.RoleName == "MANAGER")?.Count ?? 0,
            AccountantCount = roleCounts.FirstOrDefault(role => role.RoleName == "ACCOUNTANT")?.Count ?? 0,
            StaffCount = roleCounts.FirstOrDefault(role => role.RoleName == "STAFF")?.Count ?? 0,
            TotalRegions = await _context.Regions.AsNoTracking().CountAsync(),
            TotalBranches = await _context.Branches.AsNoTracking().CountAsync(),
            ActiveBranches = await _context.Branches.AsNoTracking()
                .CountAsync(branch => branch.Status == "ACTIVE"),
            PendingPayrolls = await _context.Payrolls.AsNoTracking()
                .CountAsync(payroll => payroll.Status == "SUBMITTED"),
            PendingRequests = await _context.EmployeeRequests.AsNoTracking()
                .CountAsync(request => request.Status == "PENDING"),
            TodayAttendance = attendanceStatuses,
            RecentPayrolls = recentPayrolls,
            RecentRequests = recentRequests
        };
    }

}
