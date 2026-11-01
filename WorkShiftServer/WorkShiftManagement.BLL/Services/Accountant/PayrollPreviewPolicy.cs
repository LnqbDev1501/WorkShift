using System.Text.Json.Nodes;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayrollPreviewPolicy
{
    // Standard 24-hour review/response window from actual UTC send timestamp.
    public static DateTime StandardDeadline(DateTime utc) => utc.AddHours(24);

    public static bool Mutable(PayrollPeriod p) => p.Status is "CALCULATED" or "REVISION_REQUIRED";
    public static PayrollFeedbackRequest Root(PayrollReviewData d, PayrollFeedbackRequest row)
    {
        var seen = new HashSet<int>();
        while (row.PreviousFeedbackId.HasValue)
        {
            if (!seen.Add(row.FeedbackId)) throw new InvalidOperationException("Cycle detected in review chain.");
            var previous = d.Reviews.SingleOrDefault(f => f.FeedbackId == row.PreviousFeedbackId)
                ?? throw new InvalidOperationException("Missing previous review round.");
            if (previous.EmployeeId != row.EmployeeId) throw new InvalidOperationException("Review chain belongs to wrong employee.");
            row = previous;
        }
        return row;
    }
    public static DateOnly LocalVnDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(7));
    public static bool Leaf(PayrollReviewData d, PayrollFeedbackRequest row) => !d.Reviews.Any(n => n.PreviousFeedbackId == row.FeedbackId);
    public static bool Leaf(PayrollReviewData d, PayrollCorrectionRequest row) => !d.Corrections.Any(n => n.PreviousRequestId == row.CorrectionRequestId);
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
        var leafCorrections = d.Corrections.Where(r => Leaf(d, r)).ToList();
        if (leafCorrections.Any(r => r.Status is "OPEN" or "RESPONDED" or "EXPIRED" or "RETURNED"))
            throw new InvalidOperationException("Pending Correction Request has not been completed.");
        if (d.Reviews.Any(r => r.ReviewStatus == "FEEDBACK_PENDING"))
            throw new InvalidOperationException("Pending staff feedback has not been resolved.");
        foreach (var p in Current(d))
        {
            var leaf = d.Reviews.Where(f => f.EmployeeId == p.UserId && Leaf(d, f)).OrderByDescending(f => f.FeedbackId).FirstOrDefault();
            if (leaf == null || leaf.PayrollId != p.PayrollId || leaf.ReviewStatus is not ("CONFIRMED" or "EXPIRED"))
                throw new InvalidOperationException($"{p.EmployeeFullNameSnapshot}: review round for the current Payroll version has not been completed.");
        }
    }
}
