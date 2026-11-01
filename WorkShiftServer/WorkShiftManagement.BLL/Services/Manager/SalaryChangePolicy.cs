using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Manager;

/// <summary>Calendar/rate/source rules, independent of persistence and the server's timezone.</summary>
public static class SalaryChangePolicy
{
    public static DateOnly Today(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(7));
    public static DateOnly NextMonday(DateOnly today)
        => today.AddDays(7 - (((int)today.DayOfWeek + 6) % 7));

    public static string? Text(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    public static string RequiredText(string? text)
    {
        var value = Text(text);
        if (value == null || value.Length > 1000) throw new ArgumentException("Content is required and must not exceed 1000 characters.");
        return value;
    }
    public static void Rate(decimal proposed)
    {
        if (proposed <= 0 || proposed > 9999999999999999.99m || proposed != decimal.Truncate(proposed * 100m) / 100m)
            throw new ArgumentException("Salary rate must be positive with up to 16 integer digits and 2 decimal places; no rounding permitted.");
    }
    public static void ProposedDate(DateOnly date, DateOnly today)
    {
        if (date == DateOnly.MaxValue || date.DayOfWeek != DayOfWeek.Monday || date < NextMonday(today))
            throw new ArgumentException("Effective date must be a Monday starting from next week onward (UTC+07:00).");
    }
    public static DateOnly ApprovalDate(SalaryChangeRequest item, DateOnly today, DateOnly? selected, string? changeReason)
    {
        if (item.ProposedEffectiveFrom > today)
        {
            if (selected.HasValue && selected != item.ProposedEffectiveFrom || Text(changeReason) != null)
                throw new ArgumentException("Proposed date has not arrived yet; original date must be kept and date change reason left empty.");
            ProposedDate(item.ProposedEffectiveFrom, today);
            return item.ProposedEffectiveFrom;
        }
        if (!selected.HasValue) throw new ArgumentException("Proposed date has arrived or passed; Admin must select a new effective date.");
        RequiredText(changeReason);
        ProposedDate(selected.Value, today);
        return selected.Value;
    }
    public static StaffSalaryHistory Source(User staff, int branchId, IReadOnlyCollection<StaffSalaryHistory> salaries,
        decimal proposed, DateOnly today, DateOnly effective, int? expectedId = null)
    {
        if (staff.Status != "ACTIVE" || staff.DeletedAt != null || staff.Role?.RoleName != "STAFF"
            || staff.Role.Status != "ACTIVE" || staff.BranchId != branchId || staff.Branch?.Status != "ACTIVE")
            throw new InvalidOperationException("Staff and branch must be active and within the proposal scope.");
        var rows = salaries.Where(s => s.UserId == staff.UserId && s.Status is "ACTIVE" or "INACTIVE" or "SCHEDULED").ToList();
        var current = rows.Where(s => s.EffectiveFrom <= today && (!s.EffectiveTo.HasValue || s.EffectiveTo >= today)).ToList();
        if (current.Count != 1) throw new InvalidOperationException("Missing current salary rate or overlapping salary intervals detected.");
        var source = current[0];
        if (expectedId.HasValue && source.SalaryHistoryId != expectedId)
            throw new InvalidOperationException("Baseline salary rate has changed. Manager must create a new proposal.");
        var contract = source.Contract;
        if (contract == null || contract.ContractId != source.ContractId || contract.UserId != staff.UserId
            || contract.BranchId != branchId || contract.Status != "ACTIVE" || contract.ApprovedAt == null
            || contract.StaffAcceptedAt == null || contract.StartDate > today
            || contract.EndDate.HasValue && contract.EndDate < effective
            || source.HourlyRate <= 0 || proposed <= source.HourlyRate)
            throw new InvalidOperationException("New rate must be higher than current rate; contract must be approved, accepted, and valid until effective date.");
        if (rows.Any(s => s.SalaryHistoryId != source.SalaryHistoryId && s.EffectiveFrom > today
            && s.Contract.Status != "CANCELLED"))
            throw new InvalidOperationException("A future scheduled salary rate already exists. Existing schedule must be resolved before proposing or approving again.");
        if (source.EffectiveTo.HasValue && source.EffectiveTo < effective)
            throw new InvalidOperationException("Source salary rate ends before effective date; cannot unilaterally extend validity interval.");
        return source;
    }
}
