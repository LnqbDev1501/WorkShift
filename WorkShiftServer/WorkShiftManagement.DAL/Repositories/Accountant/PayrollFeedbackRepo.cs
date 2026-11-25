using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.DAL.Repositories.Accountant;

public sealed class PayrollFeedbackRepo(AppDbContext db, ILogger<PayrollFeedbackRepo> logger) : IPayrollFeedbackRepo
{
    private readonly List<PendingRealtimeNotification> _pendingNotifications = new();
    public IReadOnlyList<PendingRealtimeNotification> PendingNotifications => _pendingNotifications;
    public void ClearPendingNotifications() => _pendingNotifications.Clear();

    public async Task<PayrollReviewData?> LoadAsync(int periodId, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.Include(p => p.Branch).SingleOrDefaultAsync(p => p.PayrollPeriodId == periodId, ct);
        if (period == null) return null;
        return new() {
            Period = period,
            Payrolls = await db.Payrolls.Where(p => p.PayrollPeriodId == periodId)
                .Include(p => p.PayrollPeriod).Include(p => p.Payslip).Include(p => p.PayrollDetails).Include(p => p.Timesheet).ToListAsync(ct),
            Reviews = await db.PayrollFeedbackRequests.Where(f => f.Payroll.PayrollPeriodId == periodId)
                .Include(f => f.Payroll).OrderBy(f => f.FeedbackId).ToListAsync(ct),
            Corrections = await db.PayrollCorrectionRequests.Where(r => r.Payroll.PayrollPeriodId == periodId)
                .Include(r => r.Payroll).OrderBy(r => r.CorrectionRequestId).ToListAsync(ct)
        };
    }
    public Task<int?> PeriodForReviewAsync(int id, CancellationToken ct) => db.PayrollFeedbackRequests.Where(f => f.FeedbackId == id).Select(f => (int?)f.Payroll.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<int?> PeriodForCorrectionAsync(int id, CancellationToken ct) => db.PayrollCorrectionRequests.Where(f => f.CorrectionRequestId == id).Select(f => (int?)f.Payroll.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<int?> PeriodForPayrollAsync(int id, CancellationToken ct) => db.Payrolls.Where(p => p.PayrollId == id).Select(p => (int?)p.PayrollPeriodId).SingleOrDefaultAsync(ct);
    public Task<List<int>> ReviewPeriodIdsAsync(int? branch, Guid? employee, CancellationToken ct) => db.PayrollPeriods.AsNoTracking()
        .Where(p => (!branch.HasValue || p.BranchId == branch) && p.Payrolls.Any(w => (!employee.HasValue || w.UserId == employee) && w.PayrollFeedbackRequests.Any()))
        .OrderByDescending(p => p.StartDate).Select(p => p.PayrollPeriodId).ToListAsync(ct);
    public async Task<List<int>> DueReviewPeriodsAsync(DateTime now, CancellationToken ct)
    {
        var feedbackPeriodIds = await db.PayrollFeedbackRequests.AsNoTracking()
            .Where(f => f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt <= now)
            .Select(f => f.Payroll.PayrollPeriodId)
            .ToListAsync(ct);

        var correctionPeriodIds = await db.PayrollCorrectionRequests.AsNoTracking()
            .Where(r => r.Status == "OPEN" && r.ResponseDeadlineAt <= now)
            .Select(r => r.Payroll.PayrollPeriodId)
            .ToListAsync(ct);

        return feedbackPeriodIds.Union(correctionPeriodIds).Distinct().ToList();
    }

    public void Add(PayrollFeedbackRequest row) => db.PayrollFeedbackRequests.Add(row);
    public Task<string?> CorrectionImpactAsync(int id, CancellationToken ct) => db.AuditLogs.AsNoTracking()
        .Where(a => a.Action == "PAYROLL_CORRECTION_SOURCE_IMPACT" && a.TableName == "payroll_correction_requests" && a.RecordId == id.ToString())
        .OrderByDescending(a => a.AuditLogId).Select(a => a.NewValue).FirstOrDefaultAsync(ct);
    public void Add(PayrollCorrectionRequest row) => db.PayrollCorrectionRequests.Add(row);
    public void Add(Payslip row) => db.Payslips.Add(row);
    public void Audit(Guid? actor, string action, string table, int id, object payload, DateTime now) => db.AuditLogs.Add(new AuditLog {
        UserId = actor, Action = action, TableName = table, RecordId = id.ToString(), NewValue = JsonSerializer.Serialize(payload), CreatedAt = now
    });

    public void NotifyInApp(int periodId, int branchId, string role, Guid? user, string title, string message, DateTime now, Guid? sender = null)
    {
        try
        {
            var senderId = (sender.HasValue && sender.Value != Guid.Empty)
                ? sender.Value
                : (db.Users.AsNoTracking()
                    .Where(u => u.Status == "ACTIVE" && u.DeletedAt == null && u.Role.RoleName == "ADMIN")
                    .Select(u => (Guid?)u.UserId)
                    .FirstOrDefault()
                    ?? db.Users.AsNoTracking()
                        .Where(u => u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => (Guid?)u.UserId)
                        .FirstOrDefault() ?? Guid.Empty);

            if (senderId == Guid.Empty)
            {
                logger.LogWarning("Cannot send payroll notification: no active admin or fallback sender found.");
                return;
            }

            var recipientIds = new List<Guid>();
            if (role == "STAFF")
            {
                if (user.HasValue && user.Value != Guid.Empty)
                {
                    var validStaff = db.Users.AsNoTracking()
                        .Where(u => u.UserId == user.Value && u.Role.RoleName == "STAFF" && u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => u.UserId)
                        .FirstOrDefault();
                    if (validStaff != Guid.Empty)
                    {
                        recipientIds.Add(validStaff);
                    }
                }
            }
            else if (role == "MANAGER")
            {
                if (user.HasValue && user.Value != Guid.Empty)
                {
                    var validManager = db.Users.AsNoTracking()
                        .Where(u => u.UserId == user.Value && u.Role.RoleName == "MANAGER" && u.BranchId == branchId && u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => u.UserId)
                        .FirstOrDefault();
                    if (validManager != Guid.Empty)
                    {
                        recipientIds.Add(validManager);
                    }
                }

                if (recipientIds.Count == 0)
                {
                    recipientIds = db.Users.AsNoTracking()
                        .Where(u => u.BranchId == branchId && u.Role.RoleName == "MANAGER" && u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => u.UserId)
                        .ToList();
                }
            }
            else if (role == "ACCOUNTANT")
            {
                if (user.HasValue && user.Value != Guid.Empty)
                {
                    var targetAccountant = db.Users.AsNoTracking()
                        .Where(u => u.UserId == user.Value && (u.Role.RoleName == "ACCOUNTANT" || u.Role.RoleName == "ADMIN") && u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => u.UserId)
                        .FirstOrDefault();
                    if (targetAccountant != Guid.Empty)
                    {
                        recipientIds.Add(targetAccountant);
                    }
                }

                if (recipientIds.Count == 0)
                {
                    recipientIds = db.Users.AsNoTracking()
                        .Where(u => (u.BranchId == branchId || u.BranchId == null) && u.Role.RoleName == "ACCOUNTANT" && u.Status == "ACTIVE" && u.DeletedAt == null)
                        .Select(u => u.UserId)
                        .ToList();
                }
            }
            else if (role == "ADMIN")
            {
                recipientIds = db.Users.AsNoTracking()
                    .Where(u => u.Role.RoleName == "ADMIN" && u.Status == "ACTIVE" && u.DeletedAt == null)
                    .Select(u => u.UserId)
                    .ToList();
            }

            if (recipientIds.Count == 0)
            {
                logger.LogWarning("No active recipients found for payroll notification: Role={Role}, TargetUser={User}, BranchId={BranchId}, PeriodId={PeriodId}", role, user, branchId, periodId);
                return;
            }

            var targetType = role switch
            {
                "STAFF" => "Staff",
                "MANAGER" => "Manager",
                "ACCOUNTANT" => "Accountant",
                _ => "All"
            };

            var notif = new WorkShiftManagement.DAL.Entities.Notification
            {
                Title = title,
                Content = message,
                NotificationType = "PAYROLL",
                CreatedBy = senderId,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = targetType,
                TargetRoles = [], // Empty array required by PostgreSQL constraint ck_notifications_target_scope
                TargetBranchId = null,
                TargetRegionId = null,
                Channels = ["InApp"],
                UserNotifications = recipientIds.Distinct().Select(id => new UserNotification
                {
                    UserId = id,
                    CreatedAt = now,
                    IsRead = false,
                    IsDeleted = false
                }).ToList()
            };

            db.Notifications.Add(notif);
            _pendingNotifications.Add(new PendingRealtimeNotification(notif, recipientIds.Distinct().ToList()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue in-app payroll notification: PeriodId={PeriodId}, Role={Role}, TargetUser={User}", periodId, role, user);
            throw;
        }
    }

    public Task<List<PayrollCorrectionRequest>> QueryIssuesAsync(int? branchId, int? periodId, string? status, CancellationToken ct)
    {
        var q = db.PayrollCorrectionRequests.AsNoTracking()
            .Include(r => r.Payroll)
                .ThenInclude(p => p.PayrollPeriod)
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            q = q.Where(r => r.Payroll.PayrollPeriod.BranchId == branchId.Value);

        if (periodId.HasValue && periodId.Value > 0)
            q = q.Where(r => r.Payroll.PayrollPeriodId == periodId.Value);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normStatus = status.Trim().ToUpperInvariant();
            q = q.Where(r => r.Status == normStatus);
        }

        return q
            .OrderByDescending(r => r.Status == "OPEN" || r.Status == "RETURNED")
            .ThenByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.CorrectionRequestId)
            .ToListAsync(ct);
    }
}
