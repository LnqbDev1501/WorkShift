using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Version.Rules;

namespace WorkShiftManagement.BLL.Helpers;

internal static class ShiftAssignmentRules
{
    internal static void ValidateCapacity(WorkShift shift, ShiftEmployeeRequirement requirement)
    {
        if (shift.ShiftAssignments.Any(a => (a.Status is "ASSIGNED" or "COMPLETED") &&
            !shift.ShiftEmployeeRequirements.Any(r => r.ShiftRequirementId == a.ShiftRequirementId)))
            throw new InvalidOperationException("An existing assignment has an invalid employee requirement.");

        var occupied = shift.ShiftAssignments.Count(a => a.ShiftRequirementId == requirement.ShiftRequirementId &&
            (a.Status is "ASSIGNED" or "COMPLETED"));
        if (occupied >= requirement.RequiredCount)
            throw new InvalidOperationException("There are no remaining slots for this employee type.");
    }

    internal static void ValidateHistory(ShiftAssignment assignment, bool restoring)
    {
        if (assignment.Attendance != null || assignment.PayslipWorkDetails.Count != 0)
            throw new InvalidOperationException("The assignment has attendance or payroll data.");
        if (assignment.LeaveRequestAssignments.Any(l => restoring || l.Request.Status is "PENDING" or "APPROVED") ||
            assignment.TimeAdjustmentRequestDetails.Any(d => restoring || d.Request.Status is "PENDING" or "APPROVED") ||
            (restoring && (assignment.ShiftSwapRequestSourceAssignments.Any() ||
                assignment.ShiftSwapRequestTargetAssignments.Any())))
            throw new InvalidOperationException("Resolve the linked employee request before changing this assignment; assignments with request history cannot be restored.");
    }

    internal static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    internal static DateTime ToUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);

    internal static void ValidateWorkingTime(WorkShift candidate, List<WorkShift> others, List<WorkingTimeRule> rules)
    {
        var first = DateOnly.FromDateTime(candidate.ScheduledStartAt.AddHours(7));
        var last = DateOnly.FromDateTime(candidate.ScheduledEndAt.AddTicks(-1).AddHours(7));
        var all = others.Append(candidate).ToList();
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var rule = WorkingTimeRuleLifecycle.SelectApplicable(rules, day)
                ?? throw new InvalidOperationException($"No working time rule applies on {day:yyyy-MM-dd}.");
            var dayStart = ToUtc(day);
            var weekStart = ToUtc(StartOfWeek(day));
            if (all.Sum(s => MinutesWithin(s, dayStart, dayStart.AddDays(1))) > rule.MaxDailyMinutes + 0.000001)
                throw new InvalidOperationException($"The assignment exceeds the daily working time limit on {day:yyyy-MM-dd}.");
            if (all.Sum(s => MinutesWithin(s, weekStart, weekStart.AddDays(7))) > rule.MaxWeeklyMinutes + 0.000001)
                throw new InvalidOperationException("The assignment exceeds the weekly working time limit.");
        }
    }

    private static double MinutesWithin(WorkShift shift, DateTime from, DateTime to)
    {
        var start = shift.ScheduledStartAt > from ? shift.ScheduledStartAt : from;
        var end = shift.ScheduledEndAt < to ? shift.ScheduledEndAt : to;
        if (end <= start) return 0;
        var duration = (shift.ScheduledEndAt - shift.ScheduledStartAt).TotalMinutes;
        if (duration <= 0)
            throw new InvalidOperationException("An existing shift has invalid working time data.");
        return (end - start).TotalMinutes;
    }

}
