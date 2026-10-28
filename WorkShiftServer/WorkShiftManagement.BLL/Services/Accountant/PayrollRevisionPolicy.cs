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
        public string? Note { get; set; }
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
}
