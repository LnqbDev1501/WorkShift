using WorkShiftManagement.BLL.Interfaces.Realtime;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.SalaryChanges;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class SalaryChangeService(
    ISalaryChangeRepo repo,
    TimeProvider clock,
    ILogger<SalaryChangeService> logger,
    INotificationRealtimePublisher? realtimePublisher = null,
    IAdminRealtimePublisher? adminRealtimePublisher = null)
    : ISalaryChangeService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private async Task DispatchRealtimeNotificationsAsync()
    {
        if (realtimePublisher == null)
        {
            repo.ClearPendingNotifications();
            return;
        }

        var pending = repo.PendingNotifications.ToList();
        repo.ClearPendingNotifications();
        foreach (var item in pending)
        {
            var realtime = new RealtimeNotificationResponse
            {
                NotificationId = item.Notification.NotificationId,
                Title = item.Notification.Title,
                Message = item.Notification.Content,
                CreatedAt = item.Notification.CreatedAt
            };

            foreach (var userId in item.RecipientUserIds)
            {
                try
                {
                    await realtimePublisher.PublishAsync(userId, realtime);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to publish realtime salary change notification to user {UserId}", userId);
                }
            }
        }
    }

    private async Task<User> Actor(Guid id, string? role, CancellationToken ct)
    {
        var actor = id == Guid.Empty ? null : await repo.ActorAsync(id, ct);
        if (actor == null || actor.Status != "ACTIVE" || actor.DeletedAt != null || actor.Role?.Status != "ACTIVE"
            || actor.Role.RoleName is not ("MANAGER" or "ADMIN") || role != null && actor.Role.RoleName != role
            || actor.Role.RoleName == "MANAGER" && (!actor.BranchId.HasValue || actor.Branch?.Status != "ACTIVE"))
            throw new UnauthorizedAccessException("User is not authorized to manage salary change proposals.");
        return actor;
    }
    private static int? Scope(User actor) => actor.Role.RoleName == "ADMIN" ? null : actor.BranchId;
    private async Task<SalaryChangeRequest> Item(User actor, int id, bool tracking, CancellationToken ct)
    {
        if (id <= 0) throw new ArgumentException("Invalid proposal ID.");
        return await repo.FindAsync(id, Scope(actor), tracking, ct)
            ?? throw new KeyNotFoundException("Salary change proposal not found within authorized scope.");
    }
    private static void Pending(SalaryChangeRequest item)
    { if (item.Status != "PENDING") throw new InvalidOperationException("Only PENDING proposals can be processed; finalized records cannot be overwritten."); }
    private static string Snapshot(SalaryChangeRequest r) => JsonSerializer.Serialize(new
    {
        r.SalaryChangeRequestId, r.UserId, r.BranchId, r.EmployeeRequestId, r.PreviousRequestId,
        r.CurrentSalaryHistoryId, r.ProposedHourlyRate, r.ProposedEffectiveFrom, r.ApprovedEffectiveFrom,
        r.EffectiveDateChangeReason, r.Reason, r.Status, r.RequestedBy, r.CreatedAt, r.ReviewedBy,
        r.ReviewedAt, r.ReviewNote, r.CancelledBy, r.CancelledAt, r.CancellationReason
    });
    private static string SalarySnapshot(StaffSalaryHistory s) => JsonSerializer.Serialize(new
    { s.SalaryHistoryId, s.UserId, s.ContractId, s.HourlyRate, s.PreviousHourlyRate, s.ChangeType,
        s.EffectiveFrom, s.EffectiveTo, s.Status, s.SalaryChangeRequestId, s.CreatedBy, s.CreatedAt });
    private async Task<SalaryChangeResponse> Map(SalaryChangeRequest r, User actor, IReadOnlyCollection<StaffSalaryHistory> salaries,
        DateOnly today, CancellationToken ct)
    {
        string? blocked = null;
        var nextMonday = SalaryChangePolicy.NextMonday(today);
        if (r.Status == "PENDING")
        {
            try
            {
                var date = r.ProposedEffectiveFrom > today ? r.ProposedEffectiveFrom : nextMonday;
                SalaryChangePolicy.ProposedDate(date, today);
                SalaryChangePolicy.Source(r.User, r.BranchId, salaries, r.ProposedHourlyRate, today, date, r.CurrentSalaryHistoryId);
                if (await repo.HasFinalPayrollAsync(r.UserId, date, ct))
                    blocked = "A payroll period covering the effective date has been submitted or closed; source salary cannot be modified.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { blocked = ex.Message; }
        }
        return new()
        {
            SalaryChangeRequestId = r.SalaryChangeRequestId, UserId = r.UserId, EmployeeName = r.User.FullName,
            BranchId = r.BranchId, BranchName = r.Branch.BranchName, EmployeeRequestId = r.EmployeeRequestId,
            PreviousRequestId = r.PreviousRequestId, CurrentSalaryHistoryId = r.CurrentSalaryHistoryId,
            ContractId = r.CurrentSalaryHistory.ContractId, ContractNumber = r.CurrentSalaryHistory.Contract.ContractNumber,
            CurrentHourlyRate = r.CurrentSalaryHistory.HourlyRate, ProposedHourlyRate = r.ProposedHourlyRate,
            ProposedEffectiveFrom = r.ProposedEffectiveFrom, ApprovedEffectiveFrom = r.ApprovedEffectiveFrom,
            EffectiveDateChangeReason = r.EffectiveDateChangeReason, Reason = r.Reason, Status = r.Status,
            RequestedBy = r.RequestedBy, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
            ReviewedBy = r.ReviewedBy, ReviewedAt = r.ReviewedAt, ReviewNote = r.ReviewNote,
            CancelledBy = r.CancelledBy, CancelledAt = r.CancelledAt, CancellationReason = r.CancellationReason,
            NewSalaryHistoryId = r.StaffSalaryHistory?.SalaryHistoryId, NewSalaryStatus = r.StaffSalaryHistory?.Status,
            CanCancel = r.Status == "PENDING" && actor.Role.RoleName == "MANAGER" && r.RequestedBy == actor.UserId,
            CanApprove = r.Status == "PENDING" && actor.Role.RoleName == "ADMIN" && blocked == null,
            CanReject = r.Status == "PENDING" && actor.Role.RoleName == "ADMIN",
            RequiresEffectiveDateChange = r.Status == "PENDING" && r.ProposedEffectiveFrom <= today,
            EarliestAllowedEffectiveFrom = nextMonday, ApprovalBlockedReason = blocked
        };
    }
    private async Task<SalaryChangeResponse> Response(User actor, int id, DateTime now, CancellationToken ct)
    {
        var item = await Item(actor, id, false, ct);
        return await Map(item, actor, await repo.SalariesAsync([item.UserId], false, ct), SalaryChangePolicy.Today(now), ct);
    }
    public async Task<PagedResponse<SalaryChangeResponse>> ListAsync(Guid id, SalaryChangeQuery q, CancellationToken ct)
    {
        var actor = await Actor(id, null, ct);
        if (q == null || q.Page < 1 || q.PageSize is < 1 or > 100 || (long)(q.Page - 1) * q.PageSize > int.MaxValue
            || q.UserId == Guid.Empty || q.BranchId <= 0 || q.Keyword?.Length > 200
            || q.CreatedFrom > q.CreatedTo || q.EffectiveFrom > q.EffectiveTo
            || q.CreatedFrom == DateOnly.MinValue || q.CreatedTo == DateOnly.MaxValue)
            throw new ArgumentException("Invalid filter or pagination parameters.");
        var status = SalaryChangePolicy.Text(q.Status)?.ToUpperInvariant();
        if (status != null && status is not ("PENDING" or "APPROVED" or "REJECTED" or "CANCELLED"))
            throw new ArgumentException("Invalid proposal status.");
        var scope = Scope(actor);
        if (scope.HasValue && q.BranchId.HasValue && scope != q.BranchId)
            throw new UnauthorizedAccessException("Managers cannot view salary proposals of other branches.");
        static DateTime UtcStart(DateOnly day) => DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
        var (items, count) = await repo.ListAsync(new(SalaryChangePolicy.Text(q.Keyword), q.UserId,
            scope ?? q.BranchId, status, q.CreatedFrom.HasValue ? UtcStart(q.CreatedFrom.Value) : null,
            q.CreatedTo.HasValue ? UtcStart(q.CreatedTo.Value.AddDays(1)) : null,
            q.EffectiveFrom, q.EffectiveTo, q.Page, q.PageSize), ct);
        var salaries = await repo.SalariesAsync(items.Select(r => r.UserId), false, ct);
        var today = SalaryChangePolicy.Today(Now);
        var result = new List<SalaryChangeResponse>();
        foreach (var r in items) result.Add(await Map(r, actor, salaries, today, ct));
        return new() { Items = result, PageNumber = q.Page, PageSize = q.PageSize, TotalItems = count };
    }
    public async Task<SalaryChangeResponse> DetailAsync(Guid actor, int id, CancellationToken ct)
        => await Response(await Actor(actor, null, ct), id, Now, ct);

    public async Task<SalaryChangeResponse> CreateAsync(Guid id, CreateSalaryChangeRequest r, CancellationToken ct)
    {
        await Actor(id, "MANAGER", ct);
        if (r == null || r.UserId == Guid.Empty || r.EmployeeRequestId <= 0 || r.PreviousRequestId <= 0)
            throw new ArgumentException("Invalid staff member or source ID.");
        SalaryChangePolicy.Rate(r.ProposedHourlyRate);
        var reason = SalaryChangePolicy.RequiredText(r.Reason);
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct);
        var now = Now; var today = SalaryChangePolicy.Today(now);
        SalaryChangePolicy.ProposedDate(r.ProposedEffectiveFrom, today);
        var staff = await repo.StaffAsync(r.UserId, actor.BranchId!.Value, ct)
            ?? throw new KeyNotFoundException("Staff member not found in Manager's branch.");
        if (await repo.PendingAsync(r.UserId, ct)) throw new InvalidOperationException("Staff member already has a PENDING salary proposal.");
        var source = SalaryChangePolicy.Source(staff, actor.BranchId.Value,
            await repo.SalariesAsync([staff.UserId], false, ct), r.ProposedHourlyRate, today, r.ProposedEffectiveFrom);
        if (await repo.HasFinalPayrollAsync(staff.UserId, r.ProposedEffectiveFrom, ct))
            throw new InvalidOperationException("A payroll period in the effective date range has already been submitted or closed.");
        if (r.EmployeeRequestId.HasValue)
        {
            var request = await repo.EmployeeRequestAsync(r.EmployeeRequestId.Value, ct);
            var detail = request?.SalaryIncreaseRequestDetail;
            if (request == null || request.UserId != staff.UserId || request.BranchId != actor.BranchId
                || request.RequestType != "SALARY_INCREASE" || request.Status != "APPROVED" || detail == null
                || detail.CurrentContractId != source.ContractId || detail.CurrentHourlyRate != source.HourlyRate)
                throw new InvalidOperationException("Staff request must be approved by Manager, match employee/branch, and source salary must remain valid.");
        }
        if (r.PreviousRequestId.HasValue)
        {
            var previous = await Item(actor, r.PreviousRequestId.Value, false, ct);
            if (previous.UserId != staff.UserId || previous.Status is not ("REJECTED" or "CANCELLED"))
                throw new InvalidOperationException("Previous proposal must belong to the same staff member and be rejected or cancelled.");
        }
        var item = new SalaryChangeRequest
        {
            UserId = staff.UserId, BranchId = actor.BranchId.Value, CurrentSalaryHistoryId = source.SalaryHistoryId,
            EmployeeRequestId = r.EmployeeRequestId, PreviousRequestId = r.PreviousRequestId,
            ProposedHourlyRate = r.ProposedHourlyRate, ProposedEffectiveFrom = r.ProposedEffectiveFrom,
            Reason = reason, Status = "PENDING", RequestedBy = id, CreatedAt = now, UpdatedAt = now
        };
        repo.Add(item); await repo.SaveAsync(ct);
        repo.Audit(id, "CREATE", "salary_change_requests", item.SalaryChangeRequestId.ToString(), null, Snapshot(item), now);

        var adminIds = await repo.GetAdminUserIdsAsync(ct);
        if (adminIds.Count > 0)
        {
            repo.EnqueueNotification(
                senderId: id,
                title: $"New Salary Increase Proposal: {staff.FullName}",
                message: $"Manager {actor.FullName} submitted a salary increase proposal for staff {staff.FullName} proposing {r.ProposedHourlyRate:N0} VND/h, effective from {r.ProposedEffectiveFrom:dd/MM/yyyy}. Reason: {reason}",
                targetType: "All",
                recipientIds: adminIds,
                now: now);
        }
        await repo.SaveAsync(ct);
        var result = await Response(actor, item.SalaryChangeRequestId, now, ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return result;
    }
    public async Task<SalaryChangeResponse> CancelAsync(Guid id, int requestId, CancelSalaryChangeRequest r, CancellationToken ct)
    {
        await Actor(id, "MANAGER", ct);
        var reason = SalaryChangePolicy.RequiredText(r?.Reason);
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "MANAGER", ct); var item = await Item(actor, requestId, true, ct);
        Pending(item);
        if (item.RequestedBy != id) throw new UnauthorizedAccessException("Only the Manager who submitted the proposal can cancel it.");
        var old = Snapshot(item); var now = Now;
        item.Status = "CANCELLED"; item.CancelledBy = id; item.CancelledAt = now;
        item.CancellationReason = reason; item.UpdatedAt = now;
        repo.Audit(id, "CANCEL", "salary_change_requests", requestId.ToString(), old, Snapshot(item), now);

        var adminIds = await repo.GetAdminUserIdsAsync(ct);
        if (adminIds.Count > 0)
        {
            repo.EnqueueNotification(
                senderId: id,
                title: $"Salary Increase Proposal #{requestId} Cancelled",
                message: $"Manager {actor.FullName} has cancelled the pending salary increase proposal #{requestId} for staff {item.User.FullName}. Reason: {reason}",
                targetType: "All",
                recipientIds: adminIds,
                now: now);
        }
        await repo.SaveAsync(ct);
        var result = await Response(actor, requestId, now, ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return result;
    }
    public async Task<SalaryChangeResponse> RejectAsync(Guid id, int requestId, RejectSalaryChangeRequest r, CancellationToken ct)
    {
        await Actor(id, "ADMIN", ct);
        var note = SalaryChangePolicy.RequiredText(r?.ReviewNote);
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "ADMIN", ct); var item = await Item(actor, requestId, true, ct);
        Pending(item); var old = Snapshot(item); var now = Now;
        item.Status = "REJECTED"; item.ReviewedBy = id; item.ReviewedAt = now; item.ReviewNote = note; item.UpdatedAt = now;
        repo.Audit(id, "REJECT", "salary_change_requests", requestId.ToString(), old, Snapshot(item), now);

        // 1. Notify Manager who requested
        repo.EnqueueNotification(
            senderId: id,
            title: $"Salary Increase Proposal #{requestId} Rejected",
            message: $"Admin has rejected salary increase proposal #{requestId} for staff {item.User.FullName}. Rejection reason: {note}",
            targetType: "Manager",
            recipientIds: [item.RequestedBy],
            now: now);

        // 2. Notify Staff
        repo.EnqueueNotification(
            senderId: id,
            title: "Salary Increase Proposal Update",
            message: $"The salary increase proposal for your account was not approved by Admin. Your current salary remains unchanged. Reason: {note}",
            targetType: "Staff",
            recipientIds: [item.UserId],
            now: now);

        await repo.SaveAsync(ct);
        var result = await Response(actor, requestId, now, ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return result;
    }
    public async Task<SalaryChangeResponse> ApproveAsync(Guid id, int requestId, ApproveSalaryChangeRequest r, CancellationToken ct)
    {
        await Actor(id, "ADMIN", ct);
        if (r == null || r.ReviewNote?.Length > 1000 || r.EffectiveDateChangeReason?.Length > 1000)
            throw new ArgumentException("Approval review note is invalid, maximum 1000 characters.");
        await using var tx = await repo.BeginChangeAsync(ct);
        var actor = await Actor(id, "ADMIN", ct); var item = await Item(actor, requestId, true, ct);
        Pending(item); var now = Now; var today = SalaryChangePolicy.Today(now);
        SalaryChangePolicy.Rate(item.ProposedHourlyRate);
        var date = SalaryChangePolicy.ApprovalDate(item, today, r.ApprovedEffectiveFrom, r.EffectiveDateChangeReason);
        var source = SalaryChangePolicy.Source(item.User, item.BranchId,
            await repo.SalariesAsync([item.UserId], true, ct), item.ProposedHourlyRate, today, date, item.CurrentSalaryHistoryId);
        if (await repo.HasFinalPayrollAsync(item.UserId, date, ct))
            throw new InvalidOperationException("Cannot modify salary source for a payroll period submitted or closed within the effective range.");
        if (item.StaffSalaryHistory != null) throw new InvalidOperationException("Proposal has already created a salary record; duplicate creation prohibited.");
        var old = Snapshot(item); var oldSalary = SalarySnapshot(source);
        var next = new StaffSalaryHistory
        {
            UserId = item.UserId, ContractId = source.ContractId, HourlyRate = item.ProposedHourlyRate,
            PreviousHourlyRate = source.HourlyRate, ChangeType = "INCREASE", EffectiveFrom = date,
            EffectiveTo = source.Contract.EndDate.HasValue
                && (!source.EffectiveTo.HasValue || source.Contract.EndDate < source.EffectiveTo)
                    ? source.Contract.EndDate : source.EffectiveTo,
            Status = "SCHEDULED", Reason = item.Reason,
            SalaryChangeRequestId = item.SalaryChangeRequestId, CreatedBy = id, CreatedAt = now
        };
        source.EffectiveTo = date.AddDays(-1); // Inclusive end date. Never overwrite the old hourly rate.
        item.Status = "APPROVED"; item.ApprovedEffectiveFrom = date;
        item.EffectiveDateChangeReason = item.ProposedEffectiveFrom <= today ? SalaryChangePolicy.Text(r.EffectiveDateChangeReason) : null;
        item.ReviewedBy = id; item.ReviewedAt = now; item.ReviewNote = SalaryChangePolicy.Text(r.ReviewNote); item.UpdatedAt = now;
        repo.Add(next);
        repo.Audit(id, "APPROVE", "salary_change_requests", requestId.ToString(), old, Snapshot(item), now);
        repo.Audit(id, "CLOSE_SALARY_INTERVAL", "staff_salary_history", source.SalaryHistoryId.ToString(), oldSalary, SalarySnapshot(source), now);
        await repo.SaveAsync(ct);
        repo.Audit(id, "CREATE_SCHEDULED_SALARY", "staff_salary_history", next.SalaryHistoryId.ToString(), null, SalarySnapshot(next), now);

        // 1. Notify Manager who requested
        repo.EnqueueNotification(
            senderId: id,
            title: $"Salary Increase Proposal #{requestId} Approved",
            message: $"Admin has approved salary increase proposal #{requestId} for staff {item.User.FullName} with rate {item.ProposedHourlyRate:N0} VND/h. Approved effective date: {date:dd/MM/yyyy}. The new rate is currently SCHEDULED and will take effect on {date:dd/MM/yyyy}." + (SalaryChangePolicy.Text(r.ReviewNote) is { } noteText ? $" Note: {noteText}" : ""),
            targetType: "Manager",
            recipientIds: [item.RequestedBy],
            now: now);

        // 2. Notify Staff
        repo.EnqueueNotification(
            senderId: id,
            title: "Salary Increase Proposal Approved",
            message: $"Your salary increase proposal has been approved with a new hourly rate of {item.ProposedHourlyRate:N0} VND/h. Please note: This rate is SCHEDULED and will officially take effect on {date:dd/MM/yyyy}. Until then, your current rate remains active.",
            targetType: "Staff",
            recipientIds: [item.UserId],
            now: now);

        await repo.SaveAsync(ct);
        var result = await Response(actor, requestId, now, ct);
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        return result;
    }

    public async Task<int> ActivateDueAsync(CancellationToken ct)
    {
        await using var tx = await repo.BeginChangeAsync(ct);
        var now = Now; var today = SalaryChangePolicy.Today(now); var count = 0;
        foreach (var salary in await repo.DueAsync(today, ct))
        {
            var proposal = salary.SalaryChangeRequest;
            var staff = salary.User; var contract = salary.Contract;
            if (proposal == null || proposal.Status != "APPROVED" || proposal.UserId != salary.UserId
                || proposal.ApprovedEffectiveFrom != salary.EffectiveFrom || proposal.ProposedHourlyRate != salary.HourlyRate
                || staff.Status != "ACTIVE" || staff.DeletedAt != null || staff.Role.RoleName != "STAFF"
                || staff.Role.Status != "ACTIVE" || staff.Branch?.Status != "ACTIVE" || staff.BranchId != proposal.BranchId
                || contract.Status != "ACTIVE" || contract.UserId != salary.UserId || contract.BranchId != proposal.BranchId
                || contract.ApprovedAt == null || contract.StaffAcceptedAt == null || contract.StartDate > today
                || contract.EndDate.HasValue && contract.EndDate < today || salary.EffectiveTo.HasValue && salary.EffectiveTo < today)
            { logger.LogWarning("Salary history {SalaryHistoryId} cannot activate because its sources are no longer eligible.", salary.SalaryHistoryId); continue; }
            var all = await repo.SalariesAsync([salary.UserId], true, ct);
            var source = all.SingleOrDefault(s => s.SalaryHistoryId == proposal.CurrentSalaryHistoryId);
            var others = all.Where(s => s.Status == "ACTIVE" && s.SalaryHistoryId != salary.SalaryHistoryId).ToList();
            if (source == null || source.ContractId != salary.ContractId || source.HourlyRate != salary.PreviousHourlyRate
                || source.EffectiveTo != salary.EffectiveFrom.AddDays(-1)
                || others.Any(s => s.SalaryHistoryId != source.SalaryHistoryId)
                || all.Any(s => s.SalaryHistoryId != salary.SalaryHistoryId && s.EffectiveFrom <= today
                    && (!s.EffectiveTo.HasValue || s.EffectiveTo >= today)))
            { logger.LogWarning("Salary history {SalaryHistoryId} has a stale or overlapping activation source.", salary.SalaryHistoryId); continue; }
            foreach (var old in others)
            {
                var oldValue = SalarySnapshot(old); old.Status = "INACTIVE";
                repo.Audit(null, "DEACTIVATE_SALARY", "staff_salary_history", old.SalaryHistoryId.ToString(), oldValue, SalarySnapshot(old), now);
            }
            await repo.SaveAsync(ct); // Flush before ACTIVE to respect the partial unique index.
            var before = SalarySnapshot(salary); salary.Status = "ACTIVE";
            repo.Audit(null, "ACTIVATE_SALARY", "staff_salary_history", salary.SalaryHistoryId.ToString(), before, SalarySnapshot(salary), now);
            await repo.SaveAsync(ct); count++;

            var senderId = salary.SalaryChangeRequest?.ReviewedBy ?? salary.CreatedBy;
            // Notify Staff
            repo.EnqueueNotification(
                senderId: senderId,
                title: "Salary Rate Activated",
                message: $"Your new hourly rate of {salary.HourlyRate:N0} VND/h is now officially ACTIVE effective from {salary.EffectiveFrom:dd/MM/yyyy}.",
                targetType: "Staff",
                recipientIds: [salary.UserId],
                now: now);

            // Notify Manager who requested if applicable
            if (salary.SalaryChangeRequest != null && salary.SalaryChangeRequest.RequestedBy != Guid.Empty)
            {
                repo.EnqueueNotification(
                    senderId: senderId,
                    title: "Staff Salary Rate Activated",
                    message: $"New hourly rate of {salary.HourlyRate:N0} VND/h for staff {salary.User.FullName} is now officially ACTIVE effective from {salary.EffectiveFrom:dd/MM/yyyy}.",
                    targetType: "Manager",
                    recipientIds: [salary.SalaryChangeRequest.RequestedBy],
                    now: now);
            }
            await repo.SaveAsync(ct);
        }
        await tx.CommitAsync(ct);
        await DispatchRealtimeNotificationsAsync();
        if (count > 0 && adminRealtimePublisher != null)
            await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                new { resource = "SalaryHistory", action = "ACTIVATED" });
        return count;
    }
}
