using System.Text.Json;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayrollRevisionPolicy
{
    public sealed class FrozenWork
    {
        public int SourceAssignmentId { get; set; }
        public int? SourceAttendanceId { get; set; }
        public DateOnly WorkDate { get; set; }
        public string ShiftNameSnapshot { get; set; } = "";
        public DateTime ScheduledStartAt { get; set; }
        public DateTime ScheduledEndAt { get; set; }
        public int BreakMinutesSnapshot { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string AttendanceStatusSnapshot { get; set; } = "";
        public int ActualWorkingMinutes { get; set; }
        public int PayableMinutes { get; set; }
        public int TotalLateMinutes { get; set; }
        public int ExcusedLateMinutes { get; set; }
        public int ChargeableLateMinutes { get; set; }
        public int TotalEarlyLeaveMinutes { get; set; }
        public int ExcusedEarlyLeaveMinutes { get; set; }
        public int ChargeableEarlyLeaveMinutes { get; set; }
    }
    public static List<FrozenWork> Work(Payroll previous)
    {
        try
        {
            using var json = JsonDocument.Parse(previous.WorkInputSnapshot ?? "null");
            var rows = JsonSerializer.Deserialize<List<FrozenWork>>(json.RootElement.GetProperty("workDetails").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Snapshot công rỗng.");
            if (json.RootElement.GetProperty("timesheetId").GetInt32() != previous.TimesheetId
                || json.RootElement.GetProperty("totalPayableMinutes").GetInt32() != rows.Sum(w => w.PayableMinutes)
                || rows.Any(w => w.WorkDate < previous.PayrollPeriod.StartDate || w.WorkDate > previous.PayrollPeriod.EndDate)
                || rows.Any(w => w.SourceAssignmentId <= 0 || w.PayableMinutes < 0 || w.PayableMinutes != w.ActualWorkingMinutes)
                || rows.Select(w => w.SourceAssignmentId).Distinct().Count() != rows.Count)
                throw new InvalidOperationException("Snapshot công không hợp lệ.");
            return rows;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        { throw new InvalidOperationException("Payroll cần snapshot công đầy đủ để tạo correction, không đọc lại Attendance hiện tại.", ex); }
    }
    public static Payroll Calculate(Payroll previous, PayrollCalculationInputs live, PayrollCorrectionRequest request, Guid actor, DateTime now)
    {
        var work = Work(previous);
        var locked = previous.Timesheet;
        if (locked.Status != "LOCKED" || locked.LockedAt == null || locked.UserId != previous.UserId
            || locked.PayrollPeriodId != previous.PayrollPeriodId)
            throw new InvalidOperationException("Payroll phải gắn với Timesheet LOCKED đúng nhân viên/kỳ.");
        var sheet = new Timesheet
        {
            TimesheetId = previous.TimesheetId, UserId = previous.UserId, PayrollPeriodId = previous.PayrollPeriodId,
            User = locked.User, Status = "LOCKED", LockedAt = locked.LockedAt, TotalAssignedShifts = work.Count,
            TotalWorkMinutes = work.Sum(w => w.PayableMinutes), TotalLateMinutes = work.Sum(w => w.TotalLateMinutes),
            ChargeableLateMinutes = work.Sum(w => w.ChargeableLateMinutes), TotalEarlyLeaveMinutes = work.Sum(w => w.TotalEarlyLeaveMinutes),
            ChargeableEarlyLeaveMinutes = work.Sum(w => w.ChargeableEarlyLeaveMinutes)
        };
        var assignments = work.Select(w => new ShiftAssignment
        {
            AssignmentId = w.SourceAssignmentId, UserId = previous.UserId, User = locked.User, UpdatedAt = locked.LockedAt.Value,
            Shift = new WorkShift { ScheduledStartAt = w.ScheduledStartAt, ScheduledEndAt = w.ScheduledEndAt,
                ShiftNameSnapshot = w.ShiftNameSnapshot, UpdatedAt = locked.LockedAt.Value },
            Attendance = new Attendance { AttendanceId = w.SourceAttendanceId ?? 0, Status = w.AttendanceStatusSnapshot,
                ShiftStartAtSnapshot = w.ScheduledStartAt, ShiftEndAtSnapshot = w.ScheduledEndAt, BreakMinutesSnapshot = w.BreakMinutesSnapshot,
                ActualWorkingMinutes = w.PayableMinutes, CheckInTime = w.CheckInTime, CheckOutTime = w.CheckOutTime,
                UpdatedAt = locked.LockedAt.Value, TotalLateMinutes = w.TotalLateMinutes, ChargeableLateMinutes = w.ChargeableLateMinutes,
                ExcusedLateMinutes = w.ExcusedLateMinutes, TotalEarlyLeaveMinutes = w.TotalEarlyLeaveMinutes,
                ChargeableEarlyLeaveMinutes = w.ChargeableEarlyLeaveMinutes, ExcusedEarlyLeaveMinutes = w.ExcusedEarlyLeaveMinutes }
        }).ToList();
        var corrections = request.AttendanceDeductions ?? throw new ArgumentException("AttendanceDeductions không được NULL.");
        if (corrections.Select(c => c.AttendancePenaltyId).Distinct().Count() != corrections.Count
            || corrections.Any(c => c.Amount < 0 || string.IsNullOrWhiteSpace(c.Reason) || c.Reason.Length > 500
                || !previous.PayrollDetails.Any(d => d.Type == "ATTENDANCE_DEDUCTION" && d.AttendancePenaltyId == c.AttendancePenaltyId)))
            throw new ArgumentException("Khoản giảm trừ sửa phải tồn tại trong Payroll cũ, không trùng, không âm và có lý do.");
        // Freeze the violation record; Accountant may correct its monetary deduction, never its minutes.
        foreach (var detail in previous.PayrollDetails.Where(d => d.Type == "ATTENDANCE_DEDUCTION"))
        {
            if (detail.AttendancePenaltyId == null || assignments.Count == 0) throw new InvalidOperationException("Thiếu nguồn giảm trừ chấm công.");
            var correction = corrections.SingleOrDefault(c => c.AttendancePenaltyId == detail.AttendancePenaltyId);
            assignments[0].Attendance!.AttendancePenaltyRecords.Add(new AttendancePenaltyRecord
            { AttendancePenaltyId = detail.AttendancePenaltyId.Value, PenaltyAmount = correction?.Amount ?? detail.Amount,
                ViolationType = detail.Description, CreatedAt = locked.LockedAt.Value });
        }
        var salaries = live.Salaries.Where(s => !request.SalaryHistoryId.HasValue || s.SalaryHistoryId == request.SalaryHistoryId).ToList();
        var rules = live.HolidayRules.Where(r => !request.HolidayPayRuleId.HasValue || r.HolidayPayRuleId == request.HolidayPayRuleId).ToList();
        if (request.SalaryHistoryId.HasValue && salaries.Count == 0 || request.HolidayPayRuleId.HasValue && rules.Count == 0)
            throw new ArgumentException("Mức lương/quy tắc được chọn không hợp lệ trong kỳ này.");
        var period = new PayrollPeriod { PayrollPeriodId = previous.PayrollPeriodId, BranchId = previous.BranchId,
            BranchNameSnapshot = previous.BranchNameSnapshot, ScheduleWeekId = live.Period.ScheduleWeekId,
            StartDate = live.Period.StartDate, EndDate = live.Period.EndDate, Status = "OPEN",
            ApprovalDeadlineAt = live.Period.Status == "APPROVED" ? null : live.Period.ApprovalDeadlineAt };
        var result = PayrollCalculationPolicy.Build(new PayrollCalculationInputs
        {
            Period = period, EmployeeIds = [previous.UserId], Timesheets = [sheet], Assignments = assignments,
            Salaries = salaries, HolidayRules = rules, Holidays = live.Holidays, Adjustments = live.Adjustments,
            CarryForwards = live.CarryForwards, UnsettledPriorEmployees = live.UnsettledPriorEmployees
        }, previous.UserId, actor, now);
        result.PreviousPayrollId = previous.PayrollId; result.VersionNo = checked(previous.VersionNo + 1);
        result.CorrectionReason = request.CorrectionReason.Trim(); result.WorkInputSnapshot = previous.WorkInputSnapshot;
        result.TotalWorkHours = previous.TotalWorkHours; result.EmployeeFullNameSnapshot = previous.EmployeeFullNameSnapshot;
        result.EmployeePositionSnapshot = previous.EmployeePositionSnapshot;
        foreach (var detail in result.PayrollDetails.Where(d => d.Type == "ATTENDANCE_DEDUCTION"))
        {
            var old = previous.PayrollDetails.Single(d => d.AttendancePenaltyId == detail.AttendancePenaltyId);
            detail.Description = old.Description;
            detail.Note = corrections.SingleOrDefault(c => c.AttendancePenaltyId == detail.AttendancePenaltyId)?.Reason.Trim() ?? old.Note;
        }
        return result;
    }
}
