using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class AdminEmployeeRequestRepo : IAdminEmployeeRequestRepo
{
    private readonly AppDbContext _context;

    public AdminEmployeeRequestRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<EmployeeRequest> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? requestType,
        string? status,
        int? branchId,
        Guid? userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeRequests.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Branch)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Include(r => r.SalaryIncreaseRequestDetail)
                .ThenInclude(sird => sird!.CurrentContract)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Where(r => r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE");

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(r => r.RequestType == requestType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (branchId.HasValue)
        {
            query = query.Where(r => r.BranchId == branchId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt < toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(r => EF.Functions.ILike(r.User.FullName, $"%{trimmedSearch}%") ||
                                     (r.Reason != null && EF.Functions.ILike(r.Reason, $"%{trimmedSearch}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmployeeRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeRequests.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Branch)
            .Include(r => r.ReviewedByNavigation)
            .Include(r => r.AppliedRequestLimitRule)
            .Include(r => r.SalaryIncreaseRequestDetail)
                .ThenInclude(sird => sird!.CurrentContract)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Include(r => r.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ScheduleWeek)
                            .ThenInclude(sw => sw.Branch)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ShiftTemplate)
            .Include(r => r.TimeAdjustmentRequestDetail)
                .ThenInclude(tard => tard!.Assignment)
                    .ThenInclude(a => a.Shift)
                        .ThenInclude(s => s.ScheduleWeek)
                            .ThenInclude(sw => sw.Branch)
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.EmployeeRequests.AsNoTracking()
            .AnyAsync(r => r.RequestId == requestId &&
                           (r.RequestType == "LEAVE" || r.RequestType == "LATE_ARRIVAL" || r.RequestType == "EARLY_LEAVE" || r.RequestType == "SALARY_INCREASE"),
                      cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException("Request not found.");
        }

        var recordIdStr = requestId.ToString();
        return await _context.AuditLogs.AsNoTracking()
            .Include(a => a.User)
                .ThenInclude(u => u!.Role)
            .Where(a => a.TableName == "employee_requests" && a.RecordId == recordIdStr)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.AuditLogId)
            .ToListAsync(cancellationToken);
    }
}
