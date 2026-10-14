using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Version.Payroll;
namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed record CarryForwardAllocation(Payroll Payroll, PayrollCarryForwardDeduction Carry, decimal Amount);
public sealed record NewCarryForward(Payroll Payroll, PayrollDetail Detail, decimal Amount);
public sealed class PayrollSettlementPlan
{
    public List<Payroll> Current { get; init; } = [];
    public List<CarryForwardAllocation> Applications { get; } = [];
    public List<NewCarryForward> NewDeductions { get; } = [];
    public List<string> BlockingReasons { get; } = [];
}
public static class PayrollSettlementPolicy
{
    public static void ValidateCarry(PayrollCarryForwardDeduction c)
    {
        var source = c.SourcePayroll; var detail = c.SourcePayrollDetail; var apps = c.PayrollCarryForwardApplications;
        if (c.OriginalAmount <= 0 || c.RemainingAmount < 0 || c.RemainingAmount > c.OriginalAmount
            || source.PayrollId != c.SourcePayrollId || source.UserId != c.UserId || source.Status != "PAID"
            || source.PayrollPeriod.Status is not ("PAID" or "CLOSED")
            || detail.PayrollDetailId != c.SourcePayrollDetailId || detail.PayrollId != c.SourcePayrollId
            || detail.Type is not ("PENALTY" or "ATTENDANCE_DEDUCTION") || detail.AppliedAmount == null
            || detail.AppliedAmount < 0 || detail.AppliedAmount > detail.Amount || detail.Amount - detail.AppliedAmount != c.OriginalAmount
            || apps.GroupBy(a => a.PayrollId).Any(g => g.Count() > 1)
            || apps.Any(a => a.CarryForwardId != c.CarryForwardId || a.AppliedAmount <= 0
                || a.Payroll.PayrollId != a.PayrollId || a.Payroll.UserId != c.UserId || a.Payroll.Status != "PAID"
                || a.Payroll.PayrollPeriod.Status is not ("PAID" or "CLOSED")
                || a.Payroll.PayrollPeriod.StartDate <= source.PayrollPeriod.EndDate
                || a.Payroll.PayrollDetails.Where(d => d.Type == "CARRY_FORWARD_DEDUCTION" && d.CarryForwardId == c.CarryForwardId)
                    .Sum(d => d.AppliedAmount ?? 0) != a.AppliedAmount)
            || c.OriginalAmount - apps.Sum(a => a.AppliedAmount) != c.RemainingAmount
            || c.Status != (c.RemainingAmount == 0 ? "SETTLED" : c.RemainingAmount == c.OriginalAmount ? "OPEN" : "PARTIALLY_APPLIED"))
            throw new InvalidOperationException($"Carry Forward #{c.CarryForwardId} sai nguồn, nhân viên, trạng thái hoặc lịch sử/số dư.");
    }

    public static PayrollSettlementPlan Assess(PayrollSettlementData data, DateTime now)
    {
        var period = data.Period;
        var plan = new PayrollSettlementPlan { Current = PayrollVersionPolicy.Current(data.Payrolls, period.Status) };
        var settled = period.Status is "PAID" or "CLOSED";
        if (!settled && period.Status != "APPROVED") plan.BlockingReasons.Add("Kỳ phải APPROVED trước khi quyết toán.");
        try { if (!settled && now < PayslipPolicy.FeedbackDeadlineAt(period.EndDate)) plan.BlockingReasons.Add("Chưa hết ngày thứ Năm để Staff phản hồi."); }
        catch (InvalidOperationException ex) { plan.BlockingReasons.Add(ex.Message); }
        if (data.UnresolvedFeedbackCount > 0) plan.BlockingReasons.Add($"Còn {data.UnresolvedFeedbackCount} feedback/correction chưa hoàn tất.");
        if (plan.Current.Count == 0 || !data.ExpectedEmployees.SetEquals(plan.Current.Select(p => p.UserId)))
            plan.BlockingReasons.Add("Chưa đủ Payroll hiện hành cho toàn bộ nhân viên cần quyết toán.");
        if (!settled && data.UnsettledPriorEmployees.Count > 0) plan.BlockingReasons.Add("Có nhân viên còn kỳ lương trước chưa quyết toán.");
        if (data.Payrolls.GroupBy(p => p.UserId).Any(g => g.OrderByDescending(p => p.VersionNo).ThenByDescending(p => p.PayrollId)
            .First().Status is "CALCULATED" or "SUBMITTED" or "REJECTED"))
            plan.BlockingReasons.Add("Còn Payroll version sửa chưa được duyệt hoàn tất.");
        foreach (var p in plan.Current)
        {
            try
            {
                if (p.Status != (settled ? "PAID" : "APPROVED") || p.PayrollPeriodId != period.PayrollPeriodId || p.BranchId != period.BranchId)
                    throw new InvalidOperationException("Payroll hiện hành chưa đúng trạng thái/chi nhánh/kỳ.");
                if (p.Payslip == null || p.Payslip.Status != "PUBLISHED" || p.Payslip.PayrollId != p.PayrollId
                    || p.Payslip.PublishedAt > now || p.Payslip.PeriodStartSnapshot != period.StartDate || p.Payslip.PeriodEndSnapshot != period.EndDate)
                    throw new InvalidOperationException("Chưa có Payslip hiện hành đã phát hợp lệ.");
                PayrollApprovalService.ValidateTotals(p);
                PayrollRevisionPolicy.Work(p);
                if (p.PayrollDetails.Any(d => d.PayrollId != p.PayrollId || d.PayrollDetailId <= 0)
                    || p.PayrollDetails.Select(d => d.PayrollDetailId).Distinct().Count() != p.PayrollDetails.Count)
                    throw new InvalidOperationException("Chi tiết tính lương phải có nguồn duy nhất và thuộc đúng Payroll.");
                var carries = data.CarryForwards.Where(c => c.UserId == p.UserId).ToList();
                foreach (var c in carries) ValidateCarry(c);
                if (settled) ValidatePaidLedger(p, carries);
                else Build(p, carries, plan);
            }
            catch (InvalidOperationException ex) { plan.BlockingReasons.Add($"{p.EmployeeFullNameSnapshot} (Payroll #{p.PayrollId}): {ex.Message}"); }
        }
        return plan;
    }

    private static void Build(Payroll p, List<PayrollCarryForwardDeduction> carries, PayrollSettlementPlan plan)
    {
        var old = carries.Where(c => c.SourcePayroll.PayrollPeriod.EndDate < p.PayrollPeriod.StartDate && c.RemainingAmount > 0)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId).ToList();
        var details = p.PayrollDetails.OrderBy(d => d.DisplayOrder).ThenBy(d => d.PayrollDetailId).ToList();
        var oldLines = details.Where(d => d.Type == "CARRY_FORWARD_DEDUCTION").ToList();
        if (!oldLines.Select(d => d.CarryForwardId).SequenceEqual(old.Select(c => (int?)c.CarryForwardId)))
            throw new InvalidOperationException("Kế hoạch Carry Forward không khớp các khoản còn dư/FIFO; cần correction Payroll.");
        var newLines = details.Where(d => d.Type is "PENALTY" or "ATTENDANCE_DEDUCTION").ToList();
        if (newLines.Any(d => d.CarryForwardId != null) || newLines.Any(d => oldLines.Any(o => o.DisplayOrder >= d.DisplayOrder)))
            throw new InvalidOperationException("Giảm trừ mới phải được áp dụng sau Carry Forward cũ.");
        var available = p.GrossSalary;
        for (var i = 0; i < old.Count; i++)
        {
            var carry = old[i]; var line = oldLines[i]; var amount = Math.Min(available, carry.RemainingAmount);
            if (line.Amount != carry.RemainingAmount || line.AppliedAmount != amount
                || carry.PayrollCarryForwardApplications.Any(a => a.PayrollId == p.PayrollId))
                throw new InvalidOperationException("Số dư/kế hoạch trừ Carry Forward đã thay đổi hoặc đã áp dụng; cần kiểm tra/correction.");
            available -= amount;
            if (amount > 0) plan.Applications.Add(new(p, carry, amount));
        }
        foreach (var line in newLines)
        {
            var applied = Math.Min(available, line.Amount);
            if (line.AppliedAmount != applied || carries.Any(c => c.SourcePayrollDetailId == line.PayrollDetailId))
                throw new InvalidOperationException("Kế hoạch giảm trừ mới không khớp hoặc đã sinh Carry Forward.");
            available -= applied;
            var deferred = line.Amount - applied;
            if (deferred > 0) plan.NewDeductions.Add(new(p, line, deferred));
        }
        if (available != p.NetSalary) throw new InvalidOperationException("Thực nhận không khớp kế hoạch quyết toán.");
    }

    private static void ValidatePaidLedger(Payroll p, List<PayrollCarryForwardDeduction> carries)
    {
        foreach (var line in p.PayrollDetails.Where(d => d.Type == "CARRY_FORWARD_DEDUCTION"))
        {
            var c = carries.SingleOrDefault(c => c.CarryForwardId == line.CarryForwardId)
                ?? throw new InvalidOperationException("Thiếu nguồn Carry Forward đã áp dụng.");
            if (c.PayrollCarryForwardApplications.Where(a => a.PayrollId == p.PayrollId).Sum(a => a.AppliedAmount) != line.AppliedAmount)
                throw new InvalidOperationException("Payroll PAID chưa có lịch sử áp dụng Carry Forward đầy đủ.");
        }
        foreach (var line in p.PayrollDetails.Where(d => d.Type is "PENALTY" or "ATTENDANCE_DEDUCTION"))
        {
            var amount = line.Amount - line.AppliedAmount!.Value;
            var c = carries.SingleOrDefault(c => c.SourcePayrollDetailId == line.PayrollDetailId);
            if (amount > 0 && (c == null || c.OriginalAmount != amount || c.SourcePayrollId != p.PayrollId)
                || amount == 0 && c != null)
                throw new InvalidOperationException("Payroll PAID chưa có dữ liệu chuyển kỳ khớp khoản giảm trừ.");
        }
    }
}
