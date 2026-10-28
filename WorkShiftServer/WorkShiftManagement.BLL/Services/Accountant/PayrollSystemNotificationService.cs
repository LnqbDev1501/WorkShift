using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;
public sealed class PayrollSystemNotificationService(IPayrollSystemEventRepo events, IPayrollFeedbackRepo reviews,
    IPayrollCalculationRepo calculation, TimeProvider clock) : IPayrollSystemNotificationService
{
    public async Task<List<PayrollSystemEvent>> ListAsync(Guid actor, int page, int size, CancellationToken ct)
    {
        var u = await calculation.GetActorAsync(actor, ct);
        if (u == null || u.Status != "ACTIVE" || u.DeletedAt != null || u.Role.Status != "ACTIVE"
            || u.Role.RoleName is not ("ADMIN" or "ACCOUNTANT" or "STAFF" or "MANAGER")
            || u.Role.RoleName is "ACCOUNTANT" or "MANAGER" && u.BranchId == null) throw new UnauthorizedAccessException("Không có quyền xem thông báo lương.");
        if (page < 1 || size is < 1 or > 100 || (long)(page - 1) * size > int.MaxValue) throw new ArgumentException("Phân trang không hợp lệ.");
        return await events.ListAsync(u.Role.RoleName, u.BranchId, actor, page, size, ct);
    }
    public async Task QueueRemindersAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime; var local = now.AddHours(7);
        foreach (var id in await events.ActivePeriodIdsAsync(ct))
        {
            await using var tx = await calculation.BeginCalculationAsync(ct);
            var d = await reviews.LoadAsync(id, ct); if (d == null) continue;
            var input = await calculation.GetInputsAsync(d.Period, ct);
            if (d.Period.Status is "OPEN" && input.EmployeeIds.Count > 0 && input.Timesheets.Count == input.EmployeeIds.Count
                && input.Timesheets.All(t => t.Status == "REVIEWED"))
                await Queue($"ready:{id}", "ACCOUNTANT", null, "Timesheet đã REVIEWED", "Có thể kiểm tra dữ liệu và tính lương toàn kỳ.");
            // At most one reminder for each object/role/slot. Keep the slots explicit and predictable.
            if (local.Hour is 9 or 14 or 20)
            {
                var slot = local.ToString("yyyyMMddHH");
                foreach (var r in d.Corrections.Where(r => r.Status is "OPEN" or "RESPONDED"))
                    await Queue($"issue:{r.CorrectionRequestId}:{r.Status}:{slot}", r.Status == "OPEN" ? "MANAGER" : "ACCOUNTANT", null,
                        "Yêu cầu sửa lương đang chờ xử lý", $"Correction Request #{r.CorrectionRequestId}: {r.Status}.");
                foreach (var f in d.Reviews.Where(f => PayrollPreviewPolicy.Leaf(d, f)))
                {
                    if (f.ReviewStatus == "PENDING" && f.ReviewDeadlineAt > now)
                        await Queue($"review:{f.FeedbackId}:{slot}", "STAFF", f.EmployeeId, "Payroll Preview đang chờ kiểm tra", $"Lượt #{f.FeedbackId}; hạn {f.ReviewDeadlineAt?.AddHours(7):dd/MM/yyyy HH:mm} (UTC+7).");
                    if (f.Status == "SUBMITTED" && f.ReviewStatus == "FEEDBACK_PENDING")
                        await Queue($"feedback:{f.FeedbackId}:{slot}", "ACCOUNTANT", null, "Staff đang chờ xử lý phản hồi lương", $"Phản hồi #{f.FeedbackId} đang SUBMITTED.");
                }
                if (d.Period.Status == "CALCULATED" && d.Payrolls.Any(p => p.Status == "CALCULATED" && !d.Reviews.Any(f => f.EmployeeId == p.UserId)))
                    await Queue($"prepare:{id}:{slot}", "ACCOUNTANT", null, "Cần kiểm tra và gửi Payroll Preview", d.Period.PeriodCode);
                if (d.Period.Status == "SUBMITTED") await Queue($"approval:{id}:{slot}", "ADMIN", null, "Kỳ lương đang chờ Admin duyệt", d.Period.PeriodCode);
            }
            await calculation.SaveAsync(ct); await tx.CommitAsync(ct);
            async Task Queue(string slot, string role, Guid? user, string title, string message)
            {
                if (await events.HasSlotAsync(slot, ct)) return;
                reviews.Audit(null, "PAYROLL_SYSTEM_EVENT", "payroll_periods", id, new PayrollSystemMessage(id, d.Period.BranchId, role, user, title, message), now);
                events.RecordSlot(slot, now);
            }
        }
    }
}
