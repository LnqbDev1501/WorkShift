using System.Text.Json.Nodes;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayrollPreviewPolicy
{
    // Store the exclusive midnight boundary; UI may describe this as through 23:59 of the preceding day.
    public static DateTime EndOfLocalDay(DateTime utc) => new DateTimeOffset(
        DateOnly.FromDateTime(utc.AddHours(7)).AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
    public static bool Mutable(PayrollPeriod p) => p.Status is "CALCULATED" or "REVISION_REQUIRED";
    public static PayrollFeedbackRequest Root(PayrollReviewData d, PayrollFeedbackRequest row)
    {
        var seen = new HashSet<int>();
        while (row.PreviousFeedbackId.HasValue)
        {
            if (!seen.Add(row.FeedbackId)) throw new InvalidOperationException("Chuỗi review có vòng lặp.");
            var previous = d.Reviews.SingleOrDefault(f => f.FeedbackId == row.PreviousFeedbackId)
                ?? throw new InvalidOperationException("Thiếu lượt review trước.");
            if (previous.EmployeeId != row.EmployeeId) throw new InvalidOperationException("Chuỗi review sai nhân viên.");
            row = previous;
        }
        return row;
    }
    public static bool Leaf(PayrollReviewData d, PayrollFeedbackRequest row) => !d.Reviews.Any(n => n.PreviousFeedbackId == row.FeedbackId);
    public static List<Payroll> Current(PayrollReviewData d) => d.Payrolls.Where(p => p.Status != "REPLACED").GroupBy(p => p.UserId)
        .Select(g => g.OrderByDescending(p => p.VersionNo).ThenByDescending(p => p.PayrollId).First()).ToList();
    public static bool Changed(Payroll before, Payroll after)
    {
        if (PayrollApprovalService.HasCalculationChange(before, after) || before.TimesheetId != after.TimesheetId || before.TotalWorkHours != after.TotalWorkHours) return true;
        try { return !JsonNode.DeepEquals(JsonNode.Parse(before.WorkInputSnapshot ?? "null")?["workDetails"], JsonNode.Parse(after.WorkInputSnapshot ?? "null")?["workDetails"]); }
        catch (System.Text.Json.JsonException) { return true; }
    }
    public static void RequireReviewed(PayrollReviewData d)
    {
        if (d.Corrections.Any(r => r.Status is "OPEN" or "RESPONDED" or "EXPIRED")) throw new InvalidOperationException("Còn Correction Request chưa hoàn tất.");
        if (d.Reviews.Any(r => r.ReviewStatus == "FEEDBACK_PENDING")) throw new InvalidOperationException("Còn phản hồi Staff chưa hoàn tất.");
        foreach (var p in Current(d))
        {
            var leaf = d.Reviews.Where(f => f.EmployeeId == p.UserId && Leaf(d, f)).OrderByDescending(f => f.FeedbackId).FirstOrDefault();
            if (leaf == null || leaf.PayrollId != p.PayrollId || leaf.ReviewStatus is not ("CONFIRMED" or "EXPIRED"))
                throw new InvalidOperationException($"{p.EmployeeFullNameSnapshot}: chưa hoàn tất review bản Payroll hiện hành.");
        }
    }
}
