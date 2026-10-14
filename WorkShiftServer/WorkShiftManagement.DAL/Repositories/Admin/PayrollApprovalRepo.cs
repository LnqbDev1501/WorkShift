using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin
{
    public class PayrollApprovalRepo : IPayrollApprovalRepo
    {
        private readonly AppDbContext _context;

        public PayrollApprovalRepo(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET LIST PAYROLL PERIOD
        // =====================================================
        public async Task<(List<PayrollPeriod> Items, int TotalCount)>
            GetPayrollPeriodsAsync(
                string? status,
                int? regionId,
                int? branchId,
                string? search,
                int pageNumber,
                int pageSize,
                DateOnly? startDateFrom = null,
                DateOnly? startDateTo = null)
        {
            var query = _context.PayrollPeriods
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.SubmittedByNavigation)
                .Include(x => x.ApprovedByNavigation)
                .Include(x => x.Payrolls)
                .Include(x => x.Timesheets)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (regionId.HasValue)
            {
                query = query.Where(x => x.Branch.RegionId == regionId.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value);
            }

            if (startDateFrom.HasValue) query = query.Where(x => x.StartDate >= startDateFrom.Value);
            if (startDateTo.HasValue) query = query.Where(x => x.StartDate <= startDateTo.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.PeriodCode,
                        keyword, "\\") ||
                    EF.Functions.ILike(
                        x.PeriodName,
                        keyword, "\\") ||
                    EF.Functions.ILike(
                        x.BranchNameSnapshot,
                        keyword, "\\"));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.PayrollPeriodId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // =====================================================
        // GET PERIOD DETAIL
        // =====================================================
        public async Task<PayrollPeriod?>
            GetPayrollPeriodDetailAsync(
                int payrollPeriodId)
        {
            return await _context.PayrollPeriods
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.CalculatedByNavigation)
                .Include(x => x.SubmittedByNavigation)
                .Include(x => x.ApprovedByNavigation)
                .Include(x => x.Payrolls)
                .Include(x => x.Timesheets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x =>
                    x.PayrollPeriodId ==
                    payrollPeriodId);
        }

        // =====================================================
        // GET PAYROLLS WITH DETAILS
        // =====================================================
        public async Task<(List<Payroll> Items, int TotalCount)>
            GetPayrollsByPeriodAsync(
                int payrollPeriodId,
                string? search,
                int pageNumber,
                int pageSize)
        {
            var query = CurrentQuery(payrollPeriodId).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.EmployeeFullNameSnapshot, keyword, "\\"));
            }

            var totalCount =
                await query.CountAsync();

            var items = await query
                .Include(x => x.RejectedByNavigation)
                .Include(x => x.PayrollDetails)
                .AsSplitQuery()
                .OrderBy(x =>
                    x.EmployeeFullNameSnapshot)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // =====================================================
        // TRACK PERIOD FOR UPDATE
        // =====================================================
        public async Task<PayrollPeriod?>
            GetPayrollPeriodForUpdateAsync(
                int payrollPeriodId)
        {
            return await _context.PayrollPeriods
                .FirstOrDefaultAsync(x => x.PayrollPeriodId == payrollPeriodId);
        }

        // =====================================================
        // TRACK ONE PAYROLL FOR UPDATE
        // =====================================================
        public async Task<Payroll?>
            GetPayrollForUpdateAsync(
                int payrollId)
        {
            return await _context.Payrolls
                .Include(x => x.PreviousPayroll).ThenInclude(x => x!.PayrollDetails)
                .Include(x => x.PayrollDetails)
                .FirstOrDefaultAsync(x => x.PayrollId == payrollId);
        }

        // =====================================================
        // TRACK ALL PAYROLLS IN PERIOD
        // =====================================================
        public async Task<List<Payroll>>
            GetAllPayrollsForUpdateAsync(
                int payrollPeriodId)
        {
            return await CurrentQuery(payrollPeriodId)
                .Include(x => x.PayrollDetails)
                .Include(x => x.Timesheet)
                .ToListAsync();
        }

        private IQueryable<Payroll> CurrentQuery(int periodId)
        {
            var candidates = _context.Payrolls.Where(x => x.PayrollPeriodId == periodId && x.Status != "REPLACED")
                .Where(x => (x.PayrollPeriod.Status != "APPROVED" && x.PayrollPeriod.Status != "PAID" && x.PayrollPeriod.Status != "CLOSED")
                    || x.Status == "APPROVED" || x.Status == "PAID");
            return candidates.Where(x => !candidates.Any(y => y.UserId == x.UserId
                && (y.VersionNo > x.VersionNo || y.VersionNo == x.VersionNo && y.PayrollId > x.PayrollId)));
        }

        public Task<Payroll?> GetLatestPayrollAsync(Guid userId, int periodId) => _context.Payrolls
            .Where(p => p.UserId == userId && p.PayrollPeriodId == periodId)
            .OrderByDescending(p => p.VersionNo).ThenByDescending(p => p.PayrollId).FirstOrDefaultAsync();

        public void Audit(Guid actor, string action, PayrollPeriod period, Payroll? payroll = null) => _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor, Action = action, TableName = payroll == null ? "payroll_periods" : "payrolls",
            RecordId = (payroll?.PayrollId ?? period.PayrollPeriodId).ToString(), CreatedAt = DateTime.UtcNow,
            NewValue = System.Text.Json.JsonSerializer.Serialize(new { period.PayrollPeriodId, PeriodStatus = period.Status,
                payroll?.PayrollId, payroll?.VersionNo, PayrollStatus = payroll?.Status, payroll?.PreviousPayrollId,
                payroll?.RejectionCategory, payroll?.RejectReason })
        });

        public Task<bool> HasApprovedCorrectionFeedbackAsync(Payroll payroll) => _context.PayrollFeedbackRequests.AnyAsync(f =>
            f.ResolvedPayrollId == payroll.PayrollId && f.Status == "APPROVED" && f.CorrectionStatus == "IN_PROGRESS"
            && f.EmployeeId == payroll.UserId && f.Payroll.UserId == payroll.UserId
            && f.Payroll.PayrollPeriodId == payroll.PayrollPeriodId && f.Payslip.Status == "PUBLISHED"
            && f.Payslip.PayrollId == f.PayrollId);

        public Task<int?> GetAccountantBranchIdAsync(Guid userId) => _context.Users.AsNoTracking()
            .Where(u => u.UserId == userId && u.Status == "ACTIVE" && u.DeletedAt == null
                && u.Role.RoleName == "ACCOUNTANT" && u.Role.Status == "ACTIVE")
            .Select(u => u.BranchId).SingleOrDefaultAsync();

        private IQueryable<Guid> ExpectedUsers(int periodId) => _context.Timesheets.Where(t => t.PayrollPeriodId == periodId)
            .Select(t => t.UserId).Union(_context.ShiftAssignments.Where(a => a.Status != "CANCELLED" && a.Shift.Status != "CANCELLED"
                && _context.PayrollPeriods.Any(p => p.PayrollPeriodId == periodId && p.ScheduleWeekId == a.Shift.ScheduleWeekId))
                .Select(a => a.UserId));

        public Task<int> GetExpectedEmployeeCountAsync(int periodId) => ExpectedUsers(periodId).Distinct().CountAsync();

        public async Task<bool> HasCompleteLockedTimesheetsAsync(int periodId, IReadOnlyCollection<Guid> payrollUsers)
        {
            var expected = await ExpectedUsers(periodId).Distinct().ToListAsync();
            var actual = payrollUsers.ToHashSet();
            if (actual.Count == 0 || !actual.SetEquals(expected)) return false;
            var locked = await _context.Timesheets.Where(t => t.PayrollPeriodId == periodId && t.Status == "LOCKED")
                .Select(t => t.UserId).Distinct().ToListAsync();
            return actual.IsSubsetOf(locked);
        }

        public async Task<bool> UserHasActiveRoleAsync(Guid userId, string roleName)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId &&
                    x.Status == "ACTIVE" && x.DeletedAt == null && x.Role.Status == "ACTIVE" && x.Role.RoleName == roleName);
        }

        public async Task<List<Guid>> GetActiveUserIdsByRoleAsync(string roleName)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.Status == "ACTIVE" && x.DeletedAt == null && x.Role.Status == "ACTIVE" && x.Role.RoleName == roleName)
                .Select(x => x.UserId)
                .ToListAsync();
        }

        // =====================================================
        // SAVE
        // =====================================================
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
