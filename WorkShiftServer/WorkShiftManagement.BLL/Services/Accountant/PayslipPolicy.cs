using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayslipPolicy
{
    public static DateTime PublicationAt(DateOnly periodEnd) => ThursdayStart(periodEnd);
    public static DateTime FeedbackDeadlineAt(DateOnly periodEnd) => ThursdayStart(periodEnd).AddDays(1);
    private static DateTime ThursdayStart(DateOnly end)
    {
        if (end.DayOfWeek != DayOfWeek.Sunday)
            throw new InvalidOperationException("Kỳ lương phải kết thúc vào Chủ nhật để xác định lịch phát thứ Năm.");
        return new DateTimeOffset(end.AddDays(4).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
    }

    public static string? FeedbackBlocked(Payslip slip, bool isOwnerStaff, bool unresolved, DateTime now)
    {
        if (!isOwnerStaff) return "Chỉ Staff sở hữu phiếu được gửi phản hồi.";
        if (slip.Status != "PUBLISHED") return "Phiếu đã được thay thế; hãy xem phiếu hiện hành.";
        if (slip.Payroll.PayrollPeriod.Status != "APPROVED" || slip.Payroll.Status != "APPROVED")
            return "Kỳ lương/Payroll không còn ở trạng thái cho phép phản hồi.";
        if (now < slip.PublishedAt) return "Phiếu chưa đến thời điểm phát hành.";
        if (now >= FeedbackDeadlineAt(slip.PeriodEndSnapshot)) return "Đã hết ngày thứ Năm; hạn phản hồi kết thúc lúc 00:00 thứ Sáu.";
        if (unresolved) return "Đang có phản hồi hoặc correction chưa xử lý xong trong kỳ này.";
        return null;
    }

    // Both initial publication and replacement use ONLY the immutable payroll work snapshot.
    public static Payslip Create(Payroll payroll, DateTime now, Payslip? previous = null)
    {
        var work = PayrollRevisionPolicy.Work(payroll);
        var period = payroll.PayrollPeriod;
        return new Payslip
        {
            PayrollId = payroll.PayrollId, Payroll = payroll,
            VersionNo = previous == null ? 1 : previous.VersionNo + 1,
            PreviousPayslipId = previous?.PayslipId, Status = "PUBLISHED", PublishedAt = now, CreatedAt = now,
            PeriodCodeSnapshot = previous?.PeriodCodeSnapshot ?? period.PeriodCode,
            PeriodNameSnapshot = previous?.PeriodNameSnapshot ?? period.PeriodName,
            PeriodStartSnapshot = previous?.PeriodStartSnapshot ?? period.StartDate,
            PeriodEndSnapshot = previous?.PeriodEndSnapshot ?? period.EndDate,
            PayslipWorkDetails = work.Select((w, index) => new PayslipWorkDetail
            {
                SourceAssignmentId = w.SourceAssignmentId, SourceAttendanceId = w.SourceAttendanceId,
                WorkDate = w.WorkDate, ShiftNameSnapshot = w.ShiftNameSnapshot, ScheduledStartAt = w.ScheduledStartAt,
                ScheduledEndAt = w.ScheduledEndAt, BreakMinutesSnapshot = w.BreakMinutesSnapshot,
                CheckInTime = w.CheckInTime, CheckOutTime = w.CheckOutTime, AttendanceStatusSnapshot = w.AttendanceStatusSnapshot,
                ActualWorkingMinutes = w.ActualWorkingMinutes, PayableMinutes = w.PayableMinutes,
                TotalLateMinutes = w.TotalLateMinutes, ExcusedLateMinutes = w.ExcusedLateMinutes,
                ChargeableLateMinutes = w.ChargeableLateMinutes, TotalEarlyLeaveMinutes = w.TotalEarlyLeaveMinutes,
                ExcusedEarlyLeaveMinutes = w.ExcusedEarlyLeaveMinutes, ChargeableEarlyLeaveMinutes = w.ChargeableEarlyLeaveMinutes,
                Note = w.Note, DisplayOrder = index + 1, CreatedAt = now
            }).ToList()
        };
    }
}
