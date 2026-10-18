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
    { if (type is not ("REWARD" or "PENALTY")) throw new ArgumentException("Loại thưởng/phạt không hợp lệ."); }
    private async Task<User> Actor(Guid id, string? requiredRole, CancellationToken ct)
    {
        var actor = await repo.ActorAsync(id, ct);
        if (id == Guid.Empty || actor == null || actor.DeletedAt != null || actor.Status != "ACTIVE" || actor.Role.Status != "ACTIVE"
            || actor.Role.RoleName is not ("ADMIN" or "MANAGER" or "ACCOUNTANT")
            || requiredRole != null && actor.Role.RoleName != requiredRole
            || actor.Role.RoleName != "ADMIN" && !actor.BranchId.HasValue)
            throw new UnauthorizedAccessException("Tài khoản không có quyền xử lý thưởng/phạt.");
        return actor;
    }
    private static int? Scope(User actor) => actor.Role.RoleName == "ADMIN" ? null : actor.BranchId;
    // Rewards/penalties only feed the payroll that has not been calculated yet. Once the accountant
    // calculates the period, adjusting a reward/penalty would invalidate the computed payroll, so the
    // whole lifecycle (create/update/delete plus approve/reject) is limited to DRAFT and OPEN periods.
    private static readonly string[] MutablePeriodStatuses = ["DRAFT", "OPEN"];
    private static bool MutablePeriod(PayrollPeriod p) => MutablePeriodStatuses.Contains(p.Status);
    private static void CheckPeriod(PayrollPeriod p)
    { if (!MutablePeriod(p)) throw new InvalidOperationException("Kỳ đã quyết toán/đóng hoặc không hợp lệ; chỉ được xem thưởng/phạt."); }
    private async Task<EmployeeRewardsPenalty> Item(int id, string type, User actor, bool tracking, CancellationToken ct)
    {
        Type(type);
        if (id <= 0) throw new ArgumentException("ID thưởng/phạt không hợp lệ.");
        return await repo.FindAsync(id, type, Scope(actor), tracking, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy khoản đúng loại thưởng/phạt trong phạm vi được phép.");
    }
    private static void Validate(SaveEmployeeAdjustmentRequest r)
    {
        if (r == null || r.UserId == Guid.Empty || r.PayrollPeriodId <= 0
            || string.IsNullOrWhiteSpace(r.AdjustmentName) || r.AdjustmentName.Trim().Length > 100
            || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length > 500)
            throw new ArgumentException("Cần Staff, kỳ lương, tên khoản (tối đa 100 ký tự) và lý do (tối đa 500 ký tự).");
        // Existing source column is NUMERIC(18,2); reject unsupported precision rather than silently round money.
        if (r.Amount <= 0 || r.Amount > 9999999999999999.99m || r.Amount != decimal.Truncate(r.Amount * 100) / 100)
            throw new ArgumentException("Số tiền phải lớn hơn 0, tối đa 16 chữ số nguyên và 2 chữ số thập phân; hệ thống không tự làm tròn.");
    }
    private async Task Targets(User actor, SaveEmployeeAdjustmentRequest r, CancellationToken ct)
    {
        var period = await repo.PeriodAsync(r.PayrollPeriodId, actor.BranchId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương thuộc chi nhánh Manager.");
        CheckPeriod(period);
        var staff = await repo.StaffAsync(r.UserId, period.BranchId, ct);
        if (staff == null || staff.DeletedAt != null || staff.Status != "ACTIVE" || staff.Role.RoleName != "STAFF" || staff.Role.Status != "ACTIVE")
            throw new ArgumentException("Nhân viên phải là Staff đang hoạt động thuộc cùng chi nhánh với kỳ lương.");
    }
    private async Task Pending(EmployeeRewardsPenalty item, CancellationToken ct)
    {
        CheckPeriod(item.PayrollPeriod);
        if (item.ApprovalStatus != "PENDING") throw new InvalidOperationException("Chỉ xử lý khoản đang PENDING; khoản đã duyệt/từ chối không được sửa, xóa hoặc duyệt lại.");
        if (item.PayrollId != null || await repo.HasPayrollSourceAsync(item.RewardPenaltyId, ct))
            throw new InvalidOperationException("Khoản đã liên kết dữ liệu Payroll; không được thay đổi nguồn trực tiếp.");
    }
    private static string Snapshot(EmployeeRewardsPenalty a) => JsonSerializer.Serialize(new
    { a.UserId, a.PayrollPeriodId, a.AdjustmentType, a.AdjustmentName, a.Amount, a.Reason, a.ApprovalStatus,
        a.ApprovedBy, a.ApprovedAt, a.RejectedBy, a.RejectedAt, a.ReviewNote, a.IsDeleted, a.DeletedBy, a.DeletedAt });
    private static EmployeeAdjustmentResponse Map(EmployeeRewardsPenalty a, User actor)
    {
        var pending = a.ApprovalStatus == "PENDING" && !a.IsDeleted && a.PayrollId == null && a.PayrollDetails.Count == 0 && MutablePeriod(a.PayrollPeriod);
        var manager = actor.Role.RoleName == "MANAGER" && actor.BranchId == a.PayrollPeriod.BranchId;
        var admin = actor.Role.RoleName == "ADMIN";
        return new()
        {
            RewardPenaltyId = a.RewardPenaltyId, AdjustmentType = a.AdjustmentType, UserId = a.UserId,
            EmployeeName = a.User.FullName, BranchId = a.PayrollPeriod.BranchId, BranchName = a.PayrollPeriod.BranchNameSnapshot,
            PayrollPeriodId = a.PayrollPeriodId, PeriodCode = a.PayrollPeriod.PeriodCode, PeriodName = a.PayrollPeriod.PeriodName,
            PeriodStart = a.PayrollPeriod.StartDate, PeriodEnd = a.PayrollPeriod.EndDate, PeriodStatus = a.PayrollPeriod.Status,
            AdjustmentName = a.AdjustmentName, Amount = a.Amount, Reason = a.Reason, ApprovalStatus = a.ApprovalStatus ?? "",
            CreatedBy = a.CreatedBy, CreatedByName = a.CreatedByNavigation?.FullName, CreatedAt = a.CreatedAt,
            ApprovedBy = a.ApprovedBy, ApprovedByName = a.ApprovedByNavigation?.FullName, ApprovedAt = a.ApprovedAt,
            RejectedBy = a.RejectedBy, RejectedByName = a.RejectedByNavigation?.FullName, RejectedAt = a.RejectedAt, ReviewNote = a.ReviewNote,
            CanUpdate = pending && manager, CanDelete = pending && manager,
            CanApprove = pending && admin && a.User.Status == "ACTIVE" && a.User.DeletedAt == null
                && a.User.BranchId == a.PayrollPeriod.BranchId && a.User.Role.RoleName == "STAFF" && a.User.Role.Status == "ACTIVE",
            CanReject = pending && admin
        };
    }
    public async Task<PagedResponse<EmployeeAdjustmentResponse>> ListAsync(Guid id, string type, EmployeeAdjustmentQuery q, CancellationToken ct)
    {
        Type(type); var actor = await Actor(id, null, ct);
        if (q == null || q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue
            || q.UserId == Guid.Empty || q.PayrollPeriodId <= 0 || q.BranchId <= 0 || q.Keyword?.Length > 200
            || q.CreatedFrom > q.CreatedTo || q.CreatedFrom == DateOnly.MinValue || q.CreatedTo == DateOnly.MaxValue)
            throw new ArgumentException("Bộ lọc hoặc phân trang không hợp lệ.");
        var status = string.IsNullOrWhiteSpace(q.ApprovalStatus) ? null : q.ApprovalStatus.Trim().ToUpperInvariant();
        if (status != null && status is not ("PENDING" or "APPROVED" or "REJECTED")) throw new ArgumentException("Trạng thái duyệt không hợp lệ.");
        var scope = Scope(actor);
        if (scope.HasValue && q.BranchId.HasValue && q.BranchId != scope) throw new UnauthorizedAccessException("Không được xem thưởng/phạt của chi nhánh khác.");
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
        var item = new EmployeeRewardsPenalty { UserId = r.UserId, PayrollPeriodId = r.PayrollPeriodId, AdjustmentType = type,
            AdjustmentName = r.AdjustmentName.Trim(), Amount = r.Amount, Reason = r.Reason.Trim(),
            ApprovalStatus = "PENDING", CreatedBy = id, CreatedAt = DateTime.UtcNow, IsDeleted = false };
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
        item.UserId = r.UserId; item.PayrollPeriodId = r.PayrollPeriodId; item.AdjustmentName = r.AdjustmentName.Trim(); item.Amount = r.Amount; item.Reason = r.Reason.Trim();
        repo.Audit(id, "UPDATE_EMPLOYEE_" + type, item, old); await repo.SaveAsync(ct);
        // Re-read names and period metadata after a possible employee/period change.
        var response = Map(await Item(itemId, type, actor, false, ct), actor);
        await tx.CommitAsync(ct); return response;
    }
    public async Task DeleteAsync(Guid id, string type, int itemId, CancellationToken ct)
    {
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); var old = Snapshot(item);
        item.IsDeleted = true; item.DeletedBy = id; item.DeletedAt = DateTime.UtcNow; item.DeletionReason = "Manager xóa khoản đang chờ duyệt.";
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
            throw new ArgumentException("Reject bắt buộc lý do; ghi chú duyệt/từ chối tối đa 1000 ký tự.");
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "ADMIN", ct); var item = await Item(itemId, type, actor, true, ct);
        await Pending(item, ct); var old = Snapshot(item); var now = DateTime.UtcNow;
        if (approve)
        {
            var source = new SaveEmployeeAdjustmentRequest { UserId = item.UserId, PayrollPeriodId = item.PayrollPeriodId,
                AdjustmentName = item.AdjustmentName, Amount = item.Amount, Reason = item.Reason };
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
