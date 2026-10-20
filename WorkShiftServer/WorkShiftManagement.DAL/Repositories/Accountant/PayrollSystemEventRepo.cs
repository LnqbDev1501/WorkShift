using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.DAL.Repositories.Accountant;
public sealed class PayrollSystemEventRepo(AppDbContext db) : IPayrollSystemEventRepo
{
    public Task<List<PayrollSystemEvent>> ListAsync(string role, int? branch, Guid user, int page, int size, CancellationToken ct)
    {
        // audit_logs is the durable system-event history, separate from Admin-authored Notifications.
        var offset = (page - 1) * size;
        return db.Database.SqlQuery<PayrollSystemEvent>($"""
            SELECT audit_log_id AS "EventId", created_at AS "CreatedAt", new_value::text AS "Payload"
            FROM public.audit_logs
            WHERE action = 'PAYROLL_SYSTEM_EVENT'
              AND new_value::jsonb->>'AudienceRole' = {role}
              AND ((new_value::jsonb->>'UserId' IS NOT NULL AND new_value::jsonb->>'UserId' = {user.ToString()})
                OR (new_value::jsonb->>'UserId' IS NULL AND ({role} = 'ADMIN' OR new_value::jsonb->>'BranchId' = {branch.ToString()})))
            ORDER BY created_at DESC, audit_log_id DESC LIMIT {size} OFFSET {offset}
            """).ToListAsync(ct);
    }
    public Task<List<PayrollSystemEvent>> PendingAsync(CancellationToken ct) => db.AuditLogs.AsNoTracking()
        .Where(e => e.Action == "PAYROLL_SYSTEM_EVENT" && !db.AuditLogs.Any(d => d.Action == "PAYROLL_EVENT_DELIVERED" && d.RecordId == e.AuditLogId.ToString()))
        .OrderBy(e => e.AuditLogId).Take(100).Select(e => new PayrollSystemEvent { EventId = e.AuditLogId, CreatedAt = e.CreatedAt, Payload = e.NewValue! }).ToListAsync(ct);
    public Task<bool> IsDeliveredAsync(long id, CancellationToken ct) => db.AuditLogs.AnyAsync(a => a.Action == "PAYROLL_EVENT_DELIVERED" && a.RecordId == id.ToString(), ct);
    public void Delivered(long id, DateTime now) => db.AuditLogs.Add(new AuditLog { Action = "PAYROLL_EVENT_DELIVERED", TableName = "audit_logs", RecordId = id.ToString(), CreatedAt = now });
    public Task<List<int>> ActivePeriodIdsAsync(CancellationToken ct) => db.PayrollPeriods.AsNoTracking().Where(p => p.Status == "OPEN" || p.Status == "CALCULATED" || p.Status == "REVISION_REQUIRED" || p.Status == "SUBMITTED")
        .Select(p => p.PayrollPeriodId).ToListAsync(ct);
    public Task<bool> HasSlotAsync(string slot, CancellationToken ct) => db.AuditLogs.AnyAsync(a => a.Action == "PAYROLL_REMINDER_SLOT" && a.RecordId == slot, ct);
    public void RecordSlot(string slot, DateTime now) => db.AuditLogs.Add(new AuditLog { Action = "PAYROLL_REMINDER_SLOT", RecordId = slot, CreatedAt = now });
}
