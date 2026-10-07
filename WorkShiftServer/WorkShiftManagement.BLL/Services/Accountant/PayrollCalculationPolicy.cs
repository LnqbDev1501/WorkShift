using System.Text.Json;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

/// <summary>Pure calculation: does not consume carry-forward balances or change locked work.</summary>
public static class PayrollCalculationPolicy
{
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(7);
    private static DateOnly WorkDate(DateTime start) => DateOnly.FromDateTime(start.Add(LocalOffset));

    private static StaffSalaryHistory Salary(PayrollCalculationInputs input, Guid user, DateOnly date)
    {
        // INACTIVE history is still authoritative for dates in its effective interval.
        var matches = input.Salaries.Where(s => s.UserId == user
            && s.Status is "ACTIVE" or "INACTIVE" or "SCHEDULED"
            && s.EffectiveFrom <= date && (s.EffectiveTo == null || s.EffectiveTo >= date)).ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException($"Ngày {date:dd/MM/yyyy}: thiếu mức lương hoặc có nhiều khoảng lương chồng lấn.");
        var salary = matches[0];
        if (salary.HourlyRate <= 0 || salary.Contract.UserId != user || salary.Contract.BranchId != input.Period.BranchId
            || salary.Contract.ApprovedAt == null || salary.Contract.StartDate > date
            || salary.Contract.EndDate.HasValue && salary.Contract.EndDate < date)
            throw new InvalidOperationException($"Ngày {date:dd/MM/yyyy}: mức lương/hợp đồng không hợp lệ cho nhân viên và chi nhánh này.");
        return salary;
    }

    // The supplied database uses unconstrained NUMERIC. Preserve decimal results; do not round intermediate hours.
    private static decimal Money(decimal value)
    {
        if (value < 0) throw new InvalidOperationException("Giá trị tiền không được âm.");
        return value;
    }

    public static Payroll Build(PayrollCalculationInputs input, Guid user, Guid actor, DateTime now)
    {
        var period = input.Period;
        if (period.Status is not ("OPEN" or "CALCULATED"))
            throw new InvalidOperationException("Chỉ tính lương ban đầu khi kỳ đang OPEN hoặc CALCULATED.");
        if (period.ScheduleWeekId == null || period.StartDate > period.EndDate)
            throw new InvalidOperationException("Kỳ lương chưa có tuần làm việc/ngày hợp lệ.");
        if (period.ApprovalDeadlineAt.HasValue && now >= period.ApprovalDeadlineAt)
            throw new InvalidOperationException("Đã hết hạn xử lý trước lịch phát phiếu của kỳ lương.");
        if (!input.EmployeeIds.Contains(user)) throw new KeyNotFoundException("Nhân viên không thuộc kỳ lương.");
        if (input.UnsettledPriorEmployees.Contains(user))
            throw new InvalidOperationException("Kỳ lương trước của nhân viên chưa quyết toán; chưa thể xác định Carry Forward chính thức.");
        if (input.Payrolls.Any(p => p.UserId == user))
            throw new InvalidOperationException("Nhân viên đã có Payroll. Payroll bị reject phải xử lý bằng luồng correction/version riêng.");
        var sheets = input.Timesheets.Where(t => t.UserId == user).ToList();
        if (sheets.Count != 1 || sheets[0].Status != "LOCKED" || sheets[0].LockedAt == null)
            throw new InvalidOperationException("Nhân viên phải có đúng một Timesheet LOCKED và thời điểm khóa.");
        var sheet = sheets[0];
        if (sheet.User.BranchId != period.BranchId || sheet.TotalWorkMinutes < 0 || sheet.IncompleteShiftCount != 0)
            throw new InvalidOperationException("Timesheet sai chi nhánh, phút công không hợp lệ hoặc còn ca chưa hoàn tất.");
        var assignments = input.Assignments.Where(a => a.UserId == user)
            .OrderBy(a => a.Shift.ScheduledStartAt).ThenBy(a => a.AssignmentId).ToList();
        if (assignments.Count != sheet.TotalAssignedShifts || assignments.Any(a => a.Attendance == null))
            throw new InvalidOperationException("Chi tiết ca/chấm công không đầy đủ so với Timesheet đã khóa.");
        if (assignments.Any(a => a.Attendance!.ActualWorkingMinutes < 0
            || a.Attendance.UpdatedAt > sheet.LockedAt || a.UpdatedAt > sheet.LockedAt
            || a.Shift.UpdatedAt > sheet.LockedAt
            || a.Attendance.AttendancePenaltyRecords.Any(p => p.CreatedAt > sheet.LockedAt)))
            throw new InvalidOperationException("Dữ liệu ca/chấm công đã thay đổi sau khi khóa hoặc có phút công không hợp lệ; không thể dùng làm snapshot.");
        if (assignments.Sum(a => (long)a.Attendance!.ActualWorkingMinutes) != sheet.TotalWorkMinutes
            || assignments.Sum(a => (long)a.Attendance!.TotalLateMinutes) != sheet.TotalLateMinutes
            || assignments.Sum(a => (long)a.Attendance!.ChargeableLateMinutes) != sheet.ChargeableLateMinutes
            || assignments.Sum(a => (long)a.Attendance!.TotalEarlyLeaveMinutes) != sheet.TotalEarlyLeaveMinutes
            || assignments.Sum(a => (long)a.Attendance!.ChargeableEarlyLeaveMinutes) != sheet.ChargeableEarlyLeaveMinutes)
            throw new InvalidOperationException("Chi tiết công không khớp Timesheet LOCKED. Accountant không được tự thay đổi công.");
        var dated = assignments.Select(a => (Assignment: a, Date: WorkDate(a.Shift.ScheduledStartAt))).ToList();
        if (dated.Any(x => x.Date < period.StartDate || x.Date > period.EndDate))
            throw new InvalidOperationException("Ca làm việc nằm ngoài kỳ lương.");
        var salaryDates = dated.Select(x => x.Date).Distinct().ToList();
        if (salaryDates.Count == 0) salaryDates.Add(period.StartDate);
        var salaryByDate = salaryDates.ToDictionary(d => d, d => Salary(input, user, d));
        var firstSalary = salaryByDate.OrderBy(x => x.Key).First().Value;
        var payroll = new Payroll
        {
            UserId = user, PayrollPeriodId = period.PayrollPeriodId, BranchId = period.BranchId,
            TimesheetId = sheet.TimesheetId, SalaryHistoryId = firstSalary.SalaryHistoryId,
            EmployeeFullNameSnapshot = sheet.User.FullName, EmployeePositionSnapshot = firstSalary.Contract.EmployeePositionSnapshot,
            BranchNameSnapshot = period.BranchNameSnapshot, HourlyRateSnapshot = firstSalary.HourlyRate,
            // Hours/quantity are display summaries. All money uses the original integer minutes.
            TotalWorkHours = sheet.TotalWorkMinutes / 60m,
            Status = "CALCULATED", VersionNo = 1, CalculatedBy = actor, CalculatedAt = now,
            CreatedAt = now, UpdatedAt = now
        };
        var workSnapshots = new List<object>();
        foreach (var (assignment, date) in dated)
        {
            var attendance = assignment.Attendance!;
            var salary = salaryByDate[date];
            var holiday = input.Holidays.SingleOrDefault(h => h.IsActive && h.HolidayDate == date);
            HolidayPayRule? rule = null;
            if (holiday != null)
            {
                var applicable = input.HolidayRules.Where(r => r.Status is "ACTIVE" or "INACTIVE" or "PUBLISH"
                    && r.EffectiveFrom <= date && (r.EffectiveTo == null || date < r.EffectiveTo)).ToList();
                if (applicable.Count != 1 || applicable[0].PayMultiplier <= 0)
                    throw new InvalidOperationException($"Ngày lễ {date:dd/MM/yyyy}: thiếu quy tắc hoặc quy tắc hệ số chồng lấn.");
                rule = applicable[0];
            }
            var multiplier = rule?.PayMultiplier ?? 1m;
            var amount = Money(attendance.ActualWorkingMinutes * salary.HourlyRate * multiplier / 60m);
            payroll.PayrollDetails.Add(new PayrollDetail
            {
                Type = "WORK", ItemCode = $"WORK-{assignment.AssignmentId}",
                Description = $"{date:dd/MM/yyyy} - {assignment.Shift.ShiftNameSnapshot}",
                SalaryHistoryId = salary.SalaryHistoryId, PublicHolidayId = holiday?.PublicHolidayId,
                HolidayPayRuleId = rule?.HolidayPayRuleId, WorkFrom = date, WorkTo = date,
                Quantity = attendance.ActualWorkingMinutes / 60m, Rate = salary.HourlyRate,
                Multiplier = multiplier, Amount = amount, CreatedAt = now,
                Note = $"payable_minutes={attendance.ActualWorkingMinutes}; tiền tính trực tiếp từ phút công"
            });
            workSnapshots.Add(new
            {
                sourceAssignmentId = assignment.AssignmentId, sourceAttendanceId = attendance.AttendanceId,
                workDate = date, shiftNameSnapshot = assignment.Shift.ShiftNameSnapshot,
                scheduledStartAt = attendance.ShiftStartAtSnapshot, scheduledEndAt = attendance.ShiftEndAtSnapshot,
                breakMinutesSnapshot = attendance.BreakMinutesSnapshot,
                checkInTime = attendance.CheckInTime, checkOutTime = attendance.CheckOutTime,
                attendanceStatusSnapshot = attendance.Status, actualWorkingMinutes = attendance.ActualWorkingMinutes,
                payableMinutes = attendance.ActualWorkingMinutes, totalLateMinutes = attendance.TotalLateMinutes,
                excusedLateMinutes = attendance.ExcusedLateMinutes, chargeableLateMinutes = attendance.ChargeableLateMinutes,
                totalEarlyLeaveMinutes = attendance.TotalEarlyLeaveMinutes,
                excusedEarlyLeaveMinutes = attendance.ExcusedEarlyLeaveMinutes,
                chargeableEarlyLeaveMinutes = attendance.ChargeableEarlyLeaveMinutes,
                // Financial inputs belong in payroll_details, so RATE/REWARD corrections preserve this work snapshot.
                displayOrder = workSnapshots.Count + 1
            });
        }
        payroll.WorkInputSnapshot = JsonSerializer.Serialize(new
        {
            schemaVersion = 1, timesheetId = sheet.TimesheetId, lockedAt = sheet.LockedAt,
            totalPayableMinutes = sheet.TotalWorkMinutes, workDetails = workSnapshots
        });
        var adjustments = input.Adjustments.Where(a => a.UserId == user && !a.IsDeleted && a.ApprovalStatus == "APPROVED")
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.RewardPenaltyId).ToList();
        if (adjustments.Any(a => a.Amount < 0 || a.AdjustmentType is not ("REWARD" or "PENALTY")))
            throw new InvalidOperationException("Khoản thưởng/phạt đã duyệt không hợp lệ.");
        foreach (var reward in adjustments.Where(a => a.AdjustmentType == "REWARD"))
            payroll.PayrollDetails.Add(new PayrollDetail
            {
                Type = "REWARD", RewardPenaltyId = reward.RewardPenaltyId, ItemCode = $"REWARD-{reward.RewardPenaltyId}",
                Description = reward.AdjustmentName, Note = reward.Reason, Amount = Money(reward.Amount),
                Multiplier = 1, CreatedAt = now
            });
        payroll.WorkPay = Money(payroll.PayrollDetails.Where(d => d.Type == "WORK").Sum(d => d.Amount));
        payroll.TotalReward = Money(payroll.PayrollDetails.Where(d => d.Type == "REWARD").Sum(d => d.Amount));
        payroll.GrossSalary = Money(payroll.WorkPay + payroll.TotalReward);
        var available = payroll.GrossSalary;
        void Deduct(PayrollDetail detail)
        {
            detail.Amount = Money(detail.Amount);
            detail.AppliedAmount = Math.Min(available, detail.Amount);
            available -= detail.AppliedAmount.Value;
            detail.Multiplier = 1; detail.CreatedAt = now;
            payroll.PayrollDetails.Add(detail);
        }
        foreach (var carry in input.CarryForwards.Where(c => c.UserId == user
            && c.Status is "OPEN" or "PARTIALLY_APPLIED" && c.RemainingAmount > 0)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.CarryForwardId))
        {
            if (carry.SourcePayroll.UserId != user || carry.SourcePayroll.Status != "PAID"
                || carry.SourcePayroll.PayrollPeriodId == period.PayrollPeriodId
                || carry.SourcePayroll.PayrollPeriod.EndDate >= period.StartDate
                || carry.SourcePayrollDetail.PayrollId != carry.SourcePayrollId
                || carry.SourcePayrollDetail.PayrollDetailId != carry.SourcePayrollDetailId
                || carry.RemainingAmount > carry.OriginalAmount)
                throw new InvalidOperationException("Carry Forward sai nhân viên/nguồn Payroll hoặc số dư không hợp lệ.");
            Deduct(new PayrollDetail { Type = "CARRY_FORWARD_DEDUCTION", CarryForwardId = carry.CarryForwardId,
                ItemCode = $"CF-{carry.CarryForwardId}", Description = carry.Reason, Amount = carry.RemainingAmount });
        }
        // Deterministic order within NEW deductions: Manager penalties, then attendance penalties; oldest first in each group.
        foreach (var penalty in adjustments.Where(a => a.AdjustmentType == "PENALTY"))
            Deduct(new PayrollDetail { Type = "PENALTY", RewardPenaltyId = penalty.RewardPenaltyId,
                ItemCode = $"PENALTY-{penalty.RewardPenaltyId}", Description = penalty.AdjustmentName,
                Note = penalty.Reason, Amount = penalty.Amount });
        foreach (var penalty in assignments.SelectMany(a => a.Attendance!.AttendancePenaltyRecords)
            .OrderBy(p => p.CreatedAt).ThenBy(p => p.AttendancePenaltyId))
            Deduct(new PayrollDetail { Type = "ATTENDANCE_DEDUCTION", AttendancePenaltyId = penalty.AttendancePenaltyId,
                ItemCode = $"ATTENDANCE-{penalty.AttendancePenaltyId}",
                Description = $"{penalty.ViolationType}: {penalty.ViolationMinutes} phút", Amount = penalty.PenaltyAmount });
        payroll.TotalPenalty = Money(payroll.PayrollDetails.Where(d => d.Type == "PENALTY").Sum(d => d.Amount));
        payroll.AttendanceDeduction = Money(payroll.PayrollDetails.Where(d => d.Type == "ATTENDANCE_DEDUCTION").Sum(d => d.Amount));
        payroll.CarryForwardDeduction = payroll.PayrollDetails.Where(d => d.Type == "CARRY_FORWARD_DEDUCTION").Sum(d => d.AppliedAmount ?? 0);
        payroll.DeferredDeductionAmount = payroll.PayrollDetails.Where(d => d.Type is "PENALTY" or "ATTENDANCE_DEDUCTION")
            .Sum(d => d.Amount - (d.AppliedAmount ?? 0));
        payroll.TotalDeduction = payroll.PayrollDetails.Sum(d => d.AppliedAmount ?? 0);
        payroll.NetSalary = available;
        var order = 0;
        foreach (var detail in payroll.PayrollDetails) detail.DisplayOrder = ++order;
        return payroll;
    }
}
