using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class EmployeeAdjustmentService(IEmployeeAdjustmentRepo repo) : IEmployeeAdjustmentService
{
    private static void Type(string type)
    { if (type is not ("REWARD" or "PENALTY")) throw new ArgumentException("Invalid reward/penalty adjustment type."); }
    
    private async Task<User> Actor(Guid id, string? requiredRole, CancellationToken ct)
    {
        var actor = await repo.ActorAsync(id, ct);
        if (id == Guid.Empty || actor == null || actor.DeletedAt != null || actor.Status != "ACTIVE" || actor.Role.Status != "ACTIVE"
            || actor.Role.RoleName is not ("ADMIN" or "MANAGER" or "ACCOUNTANT")
            || requiredRole != null && actor.Role.RoleName != requiredRole
            || actor.Role.RoleName != "ADMIN" && !actor.BranchId.HasValue)
            throw new UnauthorizedAccessException("You are not authorized to manage employee rewards/penalties.");
        return actor;
    }
    
    private static int? Scope(User actor) => actor.Role.RoleName == "ADMIN" ? null : actor.BranchId;
    
    // Rewards/penalties only feed the payroll that has not been calculated yet. Once the accountant
    // calculates the period, adjusting a reward/penalty would invalidate the computed payroll, so the
    // whole lifecycle (create/update/delete plus approve/reject) is limited to DRAFT and OPEN periods.
    private static readonly string[] MutablePeriodStatuses = ["DRAFT", "OPEN"];
    private static bool MutablePeriod(PayrollPeriod? p) => p == null || MutablePeriodStatuses.Contains(p.Status);
    private static void CheckPeriod(PayrollPeriod? p)
    { if (p != null && !MutablePeriod(p)) throw new InvalidOperationException("The payroll period is settled/closed or invalid; reward/penalty is read-only."); }

    public static (DateOnly WeekStart, DateOnly Today) GetCurrentWeekBounds()
    {
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        int diff = (7 + (int)todayVn.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var weekStart = todayVn.AddDays(-diff);
        return (weekStart, todayVn);
    }

    private async Task<EmployeeRewardsPenalty> Item(int id, string type, User actor, bool tracking, CancellationToken ct)
    {
        Type(type);
        if (id <= 0) throw new ArgumentException("Invalid reward/penalty ID.");
        return await repo.FindAsync(id, type, Scope(actor), tracking, ct)
            ?? throw new KeyNotFoundException("Employee reward/penalty record not found within your authorized scope.");
    }
    
    private static void Validate(SaveEmployeeAdjustmentRequest r)
    {
        if (r == null || r.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(r.AdjustmentName) || r.AdjustmentName.Trim().Length > 100
            || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length > 500)
            throw new ArgumentException("Please select an employee, enter an item name (max 100 chars), and enter a reason (max 500 chars).");

        var (weekStart, today) = GetCurrentWeekBounds();
        var date = r.AdjustmentDate ?? today;
        if (date < weekStart)
            throw new ArgumentException("Applicable date must be within the current week (from Monday to today).");
        if (date > today)
            throw new ArgumentException("Cannot create employee reward/penalty for a future date.");

        // Existing source column is NUMERIC(18,2); reject unsupported precision rather than silently round money.
        if (r.Amount < 1000m || r.Amount > 9999999999999999.99m || r.Amount != decimal.Truncate(r.Amount * 100) / 100)
            throw new ArgumentException("Amount must be at least 1,000 VND, max 16 integer digits and 2 decimal places.");
    }
    
    private async Task Targets(User actor, SaveEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        var branchId = Scope(actor);
        var effectiveBranchId = branchId ?? (actor.BranchId ?? 0);
        var staff = await repo.StaffAsync(r.UserId, effectiveBranchId, ct);
        if (staff == null || staff.DeletedAt != null || staff.Status != "ACTIVE" || staff.Role.RoleName != "STAFF" || staff.Role.Status != "ACTIVE")
            throw new ArgumentException("Employee must be an active staff member in the manager's branch.");

        var hasContract = await repo.StaffHasContractAsync(r.UserId, effectiveBranchId, ct);
        if (!hasContract)
            throw new ArgumentException("Reward/penalty can only be created for employees with an active contract.");

        if (r.PayrollPeriodId.HasValue && r.PayrollPeriodId.Value > 0)
        {
            var period = await repo.PeriodAsync(r.PayrollPeriodId.Value, actor.BranchId, ct);
            if (period != null) CheckPeriod(period);
        }
    }
    
    private async Task Pending(EmployeeRewardsPenalty item, CancellationToken ct)
    {
        if (item.PayrollPeriod != null) CheckPeriod(item.PayrollPeriod);
        if (item.ApprovalStatus != "PENDING") throw new InvalidOperationException("Only PENDING items can be processed; approved/rejected items cannot be edited, deleted, or re-evaluated.");
        if (item.PayrollId != null || await repo.HasPayrollSourceAsync(item.RewardPenaltyId, ct))
            throw new InvalidOperationException("Adjustment item is linked to Payroll data; direct source modification prohibited.");
    }
    
    private static string Snapshot(EmployeeRewardsPenalty a) => JsonSerializer.Serialize(new
    { a.UserId, a.PayrollPeriodId, a.AdjustmentDate, a.AdjustmentType, a.AdjustmentName, a.Amount, a.Reason, a.ApprovalStatus,
        a.ApprovedBy, a.ApprovedAt, a.RejectedBy, a.RejectedAt, a.ReviewNote, a.IsDeleted, a.DeletedBy, a.DeletedAt });
        
    private static EmployeeAdjustmentResponse Map(EmployeeRewardsPenalty a, User actor)
    {
        var pending = a.ApprovalStatus == "PENDING" && !a.IsDeleted && a.PayrollId == null && a.PayrollDetails.Count == 0 && (a.PayrollPeriod == null || MutablePeriod(a.PayrollPeriod));
        var branchId = a.PayrollPeriod?.BranchId ?? a.User?.BranchId ?? actor.BranchId ?? 0;
        var manager = actor.Role.RoleName == "MANAGER" && actor.BranchId == branchId;
        var admin = actor.Role.RoleName == "ADMIN";
        return new()
        {
            RewardPenaltyId = a.RewardPenaltyId, AdjustmentType = a.AdjustmentType, UserId = a.UserId,
            EmployeeName = a.User?.FullName ?? "", BranchId = branchId, BranchName = a.PayrollPeriod?.BranchNameSnapshot ?? a.User?.Branch?.BranchName ?? "",
            PayrollPeriodId = a.PayrollPeriodId ?? 0, PeriodCode = a.PayrollPeriod?.PeriodCode ?? "N/A", PeriodName = a.PayrollPeriod?.PeriodName ?? "Daily",
            PeriodStart = a.PayrollPeriod?.StartDate ?? (a.AdjustmentDate ?? DateOnly.MinValue), PeriodEnd = a.PayrollPeriod?.EndDate ?? (a.AdjustmentDate ?? DateOnly.MinValue),
            PeriodStatus = a.PayrollPeriod?.Status ?? "", AdjustmentDate = a.AdjustmentDate,
            AdjustmentName = a.AdjustmentName, Amount = a.Amount, Reason = a.Reason, ApprovalStatus = a.ApprovalStatus ?? "",
            CreatedBy = a.CreatedBy, CreatedByName = a.CreatedByNavigation?.FullName, CreatedAt = a.CreatedAt,
            ApprovedBy = a.ApprovedBy, ApprovedByName = a.ApprovedByNavigation?.FullName, ApprovedAt = a.ApprovedAt,
            RejectedBy = a.RejectedBy, RejectedByName = a.RejectedByNavigation?.FullName, RejectedAt = a.RejectedAt, ReviewNote = a.ReviewNote,
            CanUpdate = pending && manager, CanDelete = pending && manager,
            CanApprove = pending && admin && a.User != null && a.User.Status == "ACTIVE" && a.User.DeletedAt == null
                && (branchId == 0 || a.User.BranchId == branchId) && a.User.Role != null && a.User.Role.RoleName == "STAFF" && a.User.Role.Status == "ACTIVE",
            CanReject = pending && admin
        };
    }
    
    public async Task<PagedResponse<EmployeeAdjustmentResponse>> ListAsync(Guid id, string type, EmployeeAdjustmentQuery q, CancellationToken ct)
    {
        Type(type); var actor = await Actor(id, null, ct);
        if (q == null || q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue
            || q.UserId == Guid.Empty || q.PayrollPeriodId <= 0 || q.BranchId <= 0 || q.Keyword?.Length > 200
            || q.CreatedFrom > q.CreatedTo || q.CreatedFrom == DateOnly.MinValue || q.CreatedTo == DateOnly.MaxValue)
            throw new ArgumentException("Invalid filter or pagination parameters.");
        var status = string.IsNullOrWhiteSpace(q.ApprovalStatus) ? null : q.ApprovalStatus.Trim().ToUpperInvariant();
        if (status != null && status is not ("PENDING" or "APPROVED" or "REJECTED")) throw new ArgumentException("Invalid approval status.");
        var scope = Scope(actor);
        if (scope.HasValue && q.BranchId.HasValue && q.BranchId != scope) throw new UnauthorizedAccessException("Cannot access employee rewards/penalties from another branch.");
        var from = q.CreatedFrom.HasValue ? DateTime.SpecifyKind(q.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : (DateTime?)null;
        var to = q.CreatedTo.HasValue ? DateTime.SpecifyKind(q.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : (DateTime?)null;
        var (items, count) = await repo.ListAsync(type, new(string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim(), q.UserId,
            q.PayrollPeriodId, scope ?? q.BranchId, status, from, to, q.Page, q.PageSize), ct);
        return new() { Items = items.Select(a => Map(a, actor)).ToList(), TotalItems = count, PageNumber = q.Page, PageSize = q.PageSize };
    }
    
    public async Task<EmployeeAdjustmentResponse> DetailAsync(Guid id, string type, int itemId, CancellationToken ct)
    { var actor = await Actor(id, null, ct); return Map(await Item(itemId, type, actor, false, ct), actor); }
    
    public async Task<EmployeeAdjustmentResponse> CreateAsync(Guid id, string type, SaveEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        Type(type); Validate(r);
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct); await Targets(actor, r, ct);
        var date = r.AdjustmentDate ?? GetCurrentWeekBounds().Today;

        int? periodId = r.PayrollPeriodId is > 0 ? r.PayrollPeriodId : null;
        if (!periodId.HasValue && actor.BranchId.HasValue)
        {
            var period = await repo.FindPeriodByDateAsync(actor.BranchId.Value, date, ct);
            periodId = period?.PayrollPeriodId;
        }

        var item = new EmployeeRewardsPenalty {
            UserId = r.UserId,
            PayrollPeriodId = periodId,
            AdjustmentDate = date,
            AdjustmentType = type,
            AdjustmentName = r.AdjustmentName.Trim(),
            Amount = r.Amount,
            Reason = r.Reason.Trim(),
            ApprovalStatus = "PENDING",
            CreatedBy = id,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        repo.Add(item); await repo.SaveAsync(ct);
        repo.Audit(id, "CREATE_EMPLOYEE_" + type, item, null); await repo.SaveAsync(ct);
        var response = Map(await Item(item.RewardPenaltyId, type, actor, true, ct), actor);
        await tx.CommitAsync(ct); return response;
    }
    
    public async Task<EmployeeAdjustmentResponse> UpdateAsync(Guid id, string type, int itemId, SaveEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        Type(type); Validate(r);
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); await Targets(actor, r, ct); var old = Snapshot(item);
        var date = r.AdjustmentDate ?? item.AdjustmentDate ?? GetCurrentWeekBounds().Today;

        int? periodId = r.PayrollPeriodId is > 0 ? r.PayrollPeriodId : null;
        if (!periodId.HasValue && actor.BranchId.HasValue)
        {
            var period = await repo.FindPeriodByDateAsync(actor.BranchId.Value, date, ct);
            periodId = period?.PayrollPeriodId;
        }

        item.UserId = r.UserId;
        item.PayrollPeriodId = periodId;
        item.AdjustmentDate = date;
        item.AdjustmentName = r.AdjustmentName.Trim();
        item.Amount = r.Amount;
        item.Reason = r.Reason.Trim();
        repo.Audit(id, "UPDATE_EMPLOYEE_" + type, item, old); await repo.SaveAsync(ct);
        var response = Map(await Item(itemId, type, actor, false, ct), actor);
        await tx.CommitAsync(ct); return response;
    }
    
    public async Task DeleteAsync(Guid id, string type, int itemId, CancellationToken ct)
    {
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); var old = Snapshot(item);
        item.IsDeleted = true; item.DeletedBy = id; item.DeletedAt = DateTime.UtcNow; item.DeletionReason = "Deleted pending item by manager.";
        repo.Audit(id, "DELETE_EMPLOYEE_" + type, item, old); await repo.SaveAsync(ct); await tx.CommitAsync(ct);
    }
    
    public async Task<List<PayrollPeriodOptionResponse>> PeriodsAsync(Guid id, CancellationToken ct)
    {
        var actor = await Actor(id, "MANAGER", ct);
        var periods = await repo.PeriodsAsync(actor.BranchId!.Value, MutablePeriodStatuses, ct);
        return periods.Select(p => new PayrollPeriodOptionResponse
        {
            PayrollPeriodId = p.PayrollPeriodId, PeriodCode = p.PeriodCode, PeriodName = p.PeriodName,
            StartDate = p.StartDate, EndDate = p.EndDate, Status = p.Status
        }).ToList();
    }
    
    public async Task<EmployeeAdjustmentResponse> ReviewAsync(Guid id, string type, int itemId, bool approve, ReviewEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        if (r == null || r.ReviewNote?.Length > 1000 || !approve && string.IsNullOrWhiteSpace(r.ReviewNote))
            throw new ArgumentException("Rejection reason is required; review note cannot exceed 1000 characters.");
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "ADMIN", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); var old = Snapshot(item); var now = DateTime.UtcNow;
        if (approve)
        {
            var staff = item.User;
            if (staff == null || staff.DeletedAt != null || staff.Status != "ACTIVE"
                || staff.Role == null || staff.Role.RoleName != "STAFF" || staff.Role.Status != "ACTIVE"
                || !staff.BranchId.HasValue || staff.BranchId.Value <= 0)
                throw new ArgumentException("Employee must be an active staff member in a valid branch.");

            var staffBranchId = staff.BranchId.Value;
            var hasContract = await repo.StaffHasContractAsync(item.UserId, staffBranchId, ct);
            if (!hasContract)
                throw new ArgumentException("Reward/penalty can only be approved for employees with an active contract.");

            if (item.PayrollPeriodId.HasValue && item.PayrollPeriodId.Value > 0)
            {
                var period = item.PayrollPeriod ?? await repo.PeriodAsync(item.PayrollPeriodId.Value, null, ct);
                if (period != null)
                {
                    CheckPeriod(period);
                    if (period.BranchId != staffBranchId)
                        throw new ArgumentException("The payroll period does not belong to the employee's branch.");
                }
            }
        }
        item.ApprovalStatus = approve ? "APPROVED" : "REJECTED"; item.ReviewNote = string.IsNullOrWhiteSpace(r.ReviewNote) ? null : r.ReviewNote.Trim();
        item.ApprovedBy = approve ? id : null; item.ApprovedAt = approve ? now : null;
        item.RejectedBy = approve ? null : id; item.RejectedAt = approve ? null : now;
        repo.Audit(id, (approve ? "APPROVE_EMPLOYEE_" : "REJECT_EMPLOYEE_") + type, item, old); await repo.SaveAsync(ct);
        var response = Map(await Item(itemId, type, actor, false, ct), actor);
        await tx.CommitAsync(ct); return response;
    }
}
