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
            throw new UnauthorizedAccessException("TÃ i khoáº£n khÃ´ng cÃ³ quyá»n xá»­ lÃ½ thÆ°á»Ÿng/pháº¡t.");
        return actor;
    }
    
    private static int? Scope(User actor) => actor.Role.RoleName == "ADMIN" ? null : actor.BranchId;
    
    // Rewards/penalties only feed the payroll that has not been calculated yet. Once the accountant
    // calculates the period, adjusting a reward/penalty would invalidate the computed payroll, so the
    // whole lifecycle (create/update/delete plus approve/reject) is limited to DRAFT and OPEN periods.
    private static readonly string[] MutablePeriodStatuses = ["DRAFT", "OPEN"];
    private static bool MutablePeriod(PayrollPeriod? p) => p == null || MutablePeriodStatuses.Contains(p.Status);
    private static void CheckPeriod(PayrollPeriod? p)
    { if (p != null && !MutablePeriod(p)) throw new InvalidOperationException("Ká»³ Ä‘Ã£ quyáº¿t toÃ¡n/Ä‘Ã³ng hoáº·c khÃ´ng há»£p lá»‡; chá»‰ Ä‘Æ°á»£c xem thÆ°á»Ÿng/pháº¡t."); }

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
        if (id <= 0) throw new ArgumentException("ID thÆ°á»Ÿng/pháº¡t khÃ´ng há»£p lá»‡.");
        return await repo.FindAsync(id, type, Scope(actor), tracking, ct)
            ?? throw new KeyNotFoundException("KhÃ´ng tÃ¬m tháº¥y khoáº£n Ä‘Ãºng loáº¡i thÆ°á»Ÿng/pháº¡t trong pháº¡m vi Ä‘Æ°á»£c phÃ©p.");
    }
    
    private static void Validate(SaveEmployeeAdjustmentRequest r)
    {
        if (r == null || r.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(r.AdjustmentName) || r.AdjustmentName.Trim().Length > 100
            || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length > 500)
            throw new ArgumentException("Cáº§n chá»n nhÃ¢n viÃªn, nháº­p tÃªn khoáº£n (tá»‘i Ä‘a 100 kÃ½ tá»±) vÃ  lÃ½ do (tá»‘i Ä‘a 500 kÃ½ tá»±).");

        var (weekStart, today) = GetCurrentWeekBounds();
        var date = r.AdjustmentDate ?? today;
        if (date < weekStart)
            throw new ArgumentException("Chá»‰ Ä‘Æ°á»£c táº¡o Ä‘Æ¡n thÆ°á»Ÿng/pháº¡t cho cÃ¡c ngÃ y trong tuáº§n hiá»‡n táº¡i (tá»« thá»© Hai Ä‘áº¿n hÃ´m nay).");
        if (date > today)
            throw new ArgumentException("KhÃ´ng thá»ƒ táº¡o Ä‘Æ¡n thÆ°á»Ÿng/pháº¡t cho ngÃ y trong tÆ°Æ¡ng lai.");

        // Existing source column is NUMERIC(18,2); reject unsupported precision rather than silently round money.
        if (r.Amount <= 0 || r.Amount > 9999999999999999.99m || r.Amount != decimal.Truncate(r.Amount * 100) / 100)
            throw new ArgumentException("Sá»‘ tiá»n pháº£i lá»›n hÆ¡n 0, tá»‘i Ä‘a 16 chá»¯ sá»‘ nguyÃªn vÃ  2 chá»¯ sá»‘ tháº­p phÃ¢n; há»‡ thá»‘ng khÃ´ng tá»± lÃ m trÃ²n.");
    }
    
    private async Task Targets(User actor, SaveEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        var branchId = Scope(actor);
        var staff = await repo.StaffAsync(r.UserId, branchId ?? (actor.BranchId ?? 0), ct);
        if (staff == null || staff.DeletedAt != null || staff.Status != "ACTIVE" || staff.Role.RoleName != "STAFF" || staff.Role.Status != "ACTIVE")
            throw new ArgumentException("NhÃ¢n viÃªn pháº£i lÃ  Staff Ä‘ang hoáº¡t Ä‘á»™ng thuá»™c chi nhÃ¡nh cá»§a Manager.");

        if (r.PayrollPeriodId.HasValue && r.PayrollPeriodId.Value > 0)
        {
            var period = await repo.PeriodAsync(r.PayrollPeriodId.Value, actor.BranchId, ct);
            if (period != null) CheckPeriod(period);
        }
    }
    
    private async Task Pending(EmployeeRewardsPenalty item, CancellationToken ct)
    {
        if (item.PayrollPeriod != null) CheckPeriod(item.PayrollPeriod);
        if (item.ApprovalStatus != "PENDING") throw new InvalidOperationException("Chá»‰ xá»­ lÃ½ khoáº£n Ä‘ang PENDING; khoáº£n Ä‘Ã£ duyá»‡t/tá»« chá»‘i khÃ´ng Ä‘Æ°á»£c sá»­a, xÃ³a hoáº·c duyá»‡t láº¡i.");
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
            PayrollPeriodId = a.PayrollPeriodId ?? 0, PeriodCode = a.PayrollPeriod?.PeriodCode ?? "N/A", PeriodName = a.PayrollPeriod?.PeriodName ?? "Theo ngày",
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
            throw new ArgumentException("Bá»™ lá»c hoáº·c phÃ¢n trang khÃ´ng há»£p lá»‡.");
        var status = string.IsNullOrWhiteSpace(q.ApprovalStatus) ? null : q.ApprovalStatus.Trim().ToUpperInvariant();
        if (status != null && status is not ("PENDING" or "APPROVED" or "REJECTED")) throw new ArgumentException("Tráº¡ng thÃ¡i duyá»‡t khÃ´ng há»£p lá»‡.");
        var scope = Scope(actor);
        if (scope.HasValue && q.BranchId.HasValue && q.BranchId != scope) throw new UnauthorizedAccessException("KhÃ´ng Ä‘Æ°á»£c xem thÆ°á»Ÿng/pháº¡t cá»§a chi nhÃ¡nh khÃ¡c.");
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
        item.IsDeleted = true; item.DeletedBy = id; item.DeletedAt = DateTime.UtcNow; item.DeletionReason = "Manager xÃ³a khoáº£n Ä‘ang chá» duyá»‡t.";
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
            throw new ArgumentException("Reject báº¯t buá»™c lÃ½ do; ghi chÃº duyá»‡t/tá»« chá»‘i tá»‘i Ä‘a 1000 kÃ½ tá»±.");
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "ADMIN", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); var old = Snapshot(item); var now = DateTime.UtcNow;
        if (approve)
        {
            var source = new SaveEmployeeAdjustmentRequest {
                UserId = item.UserId,
                PayrollPeriodId = item.PayrollPeriodId,
                AdjustmentDate = item.AdjustmentDate,
                AdjustmentName = item.AdjustmentName,
                Amount = item.Amount,
                Reason = item.Reason
            };
            Validate(source); await Targets(actor, source, ct);
        }
        item.ApprovalStatus = approve ? "APPROVED" : "REJECTED"; item.ReviewNote = string.IsNullOrWhiteSpace(r.ReviewNote) ? null : r.ReviewNote.Trim();
        item.ApprovedBy = approve ? id : null; item.ApprovedAt = approve ? now : null;
        item.RejectedBy = approve ? null : id; item.RejectedAt = approve ? null : now;
        repo.Audit(id, (approve ? "APPROVE_EMPLOYEE_" : "REJECT_EMPLOYEE_") + type, item, old); await repo.SaveAsync(ct);
        var response = Map(await Item(itemId, type, actor, false, ct), actor);
        await tx.CommitAsync(ct); return response;
    }
}