using System.Text.Json;
using System.Text.Json.Nodes;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayrollSourceImpactPolicy
{
    public static bool OnlyCapturedSharedChanges(Payroll before, Payroll after, IReadOnlyCollection<string> captured)
    {
        var keys = SharedChanges(before, after);
        if (keys.Count == 0 || keys.Except(captured, StringComparer.Ordinal).Any()) return false;
        // Increasing work pay may change deduction allocation without changing its source.
        static string OtherSources(Payroll p) => JsonSerializer.Serialize(p.PayrollDetails.Where(d => d.Type != "WORK")
            .OrderBy(d => d.Type).ThenBy(d => d.ItemCode).Select(d => new { d.Type, d.ItemCode, d.RewardPenaltyId,
                d.AttendancePenaltyId, d.CarryForwardId, d.Quantity, d.Rate, d.Multiplier, d.Amount }));
        if (OtherSources(before) != OtherSources(after)) return false;
        // A coincident attendance edit is not authorized by a holiday/rate correction.
        if (before.TimesheetId != after.TimesheetId || before.TotalWorkHours != after.TotalWorkHours) return false;
        try
        {
            return JsonNode.DeepEquals(JsonNode.Parse(before.WorkInputSnapshot ?? "null")?["workDetails"],
                JsonNode.Parse(after.WorkInputSnapshot ?? "null")?["workDetails"]);
        }
        catch (JsonException) { return false; }
    }
    // A shared change key includes the old AND new values. Matching only a date/source ID
    // would incorrectly extend an old case to an unrelated later source change.
    public static List<string> SharedChanges(Payroll before, Payroll after)
    {
        var keys = new List<string>();
        foreach (var old in before.PayrollDetails.Where(d => d.Type == "WORK"))
        {
            var next = after.PayrollDetails.SingleOrDefault(d => d.Type == "WORK" && d.ItemCode == old.ItemCode);
            if (next == null) continue;
            if (old.Rate != next.Rate || old.SalaryHistoryId != next.SalaryHistoryId)
                keys.Add(JsonSerializer.Serialize(new { source = "SALARY", before.UserId, old.SalaryHistoryId, old.Rate, newId = next.SalaryHistoryId, newRate = next.Rate }));
            if (old.Multiplier != next.Multiplier || old.HolidayPayRuleId != next.HolidayPayRuleId || old.PublicHolidayId != next.PublicHolidayId)
                keys.Add(JsonSerializer.Serialize(new { source = "HOLIDAY", old.WorkFrom, old.PublicHolidayId, old.HolidayPayRuleId, old.Multiplier,
                    newHoliday = next.PublicHolidayId, newRule = next.HolidayPayRuleId, newMultiplier = next.Multiplier }));
        }
        return keys.Distinct(StringComparer.Ordinal).ToList();
    }
    public static List<string> Changes(Payroll before, Payroll after)
    {
        static string Lines(Payroll p, string type) => JsonSerializer.Serialize(p.PayrollDetails.Where(d => d.Type == type)
            .OrderBy(d => d.ItemCode).Select(d => new { d.ItemCode, d.SalaryHistoryId, d.PublicHolidayId, d.HolidayPayRuleId,
                d.RewardPenaltyId, d.AttendancePenaltyId, d.CarryForwardId, d.WorkFrom, d.WorkTo, d.Quantity, d.Rate, d.Multiplier, d.Amount, d.AppliedAmount }));
        var changes = new List<string>();
        foreach (var type in new[] { "WORK", "REWARD", "PENALTY", "ATTENDANCE_DEDUCTION", "CARRY_FORWARD_DEDUCTION" })
            if (Lines(before, type) != Lines(after, type)) changes.Add(type);
        if (PayrollPreviewPolicy.Changed(before, after) && changes.Count == 0) changes.Add("WORK_SNAPSHOT_OR_TOTALS");
        return changes;
    }
}
