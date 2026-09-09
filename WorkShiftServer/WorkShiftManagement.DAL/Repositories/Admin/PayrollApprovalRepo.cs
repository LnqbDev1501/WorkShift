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
                int pageSize)
        {
            var query = _context.PayrollPeriods
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.SubmittedByNavigation)
                .Include(x => x.ApprovedByNavigation)
                .Include(x => x.Payrolls)
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.PeriodCode,
                        keyword) ||
                    EF.Functions.ILike(
                        x.PeriodName,
                        keyword) ||
                    EF.Functions.ILike(
                        x.BranchNameSnapshot,
                        keyword));
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
            var query = _context.Payrolls
                .AsNoTracking()
                .Where(x =>
                    x.PayrollPeriodId ==
                    payrollPeriodId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.EmployeeFullNameSnapshot, keyword));
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
                .FirstOrDefaultAsync(x => x.PayrollId == payrollId);
        }

        // =====================================================
        // TRACK ALL PAYROLLS IN PERIOD
        // =====================================================
        public async Task<List<Payroll>>
            GetAllPayrollsForUpdateAsync(
                int payrollPeriodId)
        {
            return await _context.Payrolls
                .Where(x => x.PayrollPeriodId == payrollPeriodId)
                .ToListAsync();
        }

        // =====================================================
        // USER EXISTS
        // =====================================================
        public async Task<bool>
            UserExistsAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId);
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