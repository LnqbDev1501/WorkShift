using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Rules;

public static class WorkingTimeRuleLifecycle
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISH";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string Cancelled = "CANCEL";

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public static WorkingTimeRule? SelectApplicable(IEnumerable<WorkingTimeRule> rules, DateOnly workDate) =>
        rules.Where(rule =>
                (rule.Status is Active or Published || rule.Status == Inactive && rule.EffectiveTo.HasValue) &&
                rule.EffectiveFrom.HasValue && rule.EffectiveFrom.Value <= workDate &&
                (!rule.EffectiveTo.HasValue || workDate < rule.EffectiveTo.Value))
            // A published replacement takes precedence from its effective date,
            // even before the current active version has been retired.
            .OrderByDescending(rule => rule.EffectiveFrom)
            .ThenByDescending(rule => rule.WorkingTimeRuleId)
            .FirstOrDefault();

    public static void RequireStatus(WorkingTimeRule rule, string status)
    {
        if (rule.Status != status)
            throw new InvalidOperationException($"This action requires {status} status. Current status: {rule.Status}.");
    }

    public static void ValidateEffectiveDate(DateOnly? date, DateOnly today)
    {
        if (!date.HasValue || date.Value <= today)
            throw new ArgumentException("Effective date must be after today (Vietnam time).");
    }

    public static void EnsurePublicationAvailable(WorkingTimeRule? publishedVersion)
    {
        if (publishedVersion?.Status == Published)
            throw new InvalidOperationException(
                $"Version V{publishedVersion.WorkingTimeRuleId} is already published. Cancel that publication before publishing another version.");
    }

    public static void ValidateSchedule(int id, DateOnly date, IEnumerable<WorkingTimeRule> existing)
    {
        foreach (var other in existing.Where(rule => rule.WorkingTimeRuleId != id && rule.Status != Cancelled))
        {
            if (other.EffectiveFrom == date)
                throw new InvalidOperationException("Another version already uses this effective date. Choose a different date.");
            if (other.Status == Active && other.EffectiveFrom >= date)
                throw new InvalidOperationException("The effective date must be after the current active version.");
            if (other.Status == Inactive && other.EffectiveFrom <= date &&
                (!other.EffectiveTo.HasValue || other.EffectiveTo > date))
                throw new InvalidOperationException("The effective date overlaps an existing historical period.");
        }
    }

    public static void Retire(WorkingTimeRule previous, WorkingTimeRule next, DateTime now)
    {
        RequireStatus(previous, Active);
        RequireStatus(next, Published);
        if (!next.EffectiveFrom.HasValue || previous.EffectiveFrom >= next.EffectiveFrom)
            throw new InvalidOperationException("The replacement must have a later effective date.");
        previous.Status = Inactive;
        // EffectiveTo is exclusive: the old period ends exactly when the new one starts.
        previous.EffectiveTo = next.EffectiveFrom.Value;
        previous.UpdatedAt = now;
    }

    public static void Activate(WorkingTimeRule rule, DateOnly today, DateTime now)
    {
        RequireStatus(rule, Published);
        if (!rule.EffectiveFrom.HasValue || rule.EffectiveFrom.Value > today)
            throw new InvalidOperationException("This version is not due for activation.");
        rule.Status = Active;
        rule.EffectiveTo = null;
        rule.UpdatedAt = now;
    }
}
