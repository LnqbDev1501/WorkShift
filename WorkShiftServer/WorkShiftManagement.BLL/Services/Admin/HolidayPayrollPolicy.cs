using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Admin;

public static class HolidayPayrollPolicy
{
    private static HolidayPayRule? Resolve(IEnumerable<HolidayPayRule> rules, DateOnly workDate) =>
        rules.Where(r => (r.Status == "ACTIVE" || r.Status == "INACTIVE" || r.Status == "PUBLISH") && r.EffectiveFrom <= workDate && (r.EffectiveTo == null || workDate < r.EffectiveTo)).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();

    public static void Validate(IEnumerable<PayrollDetail> details, IReadOnlyList<PublicHolidayConfig> holidays,
        IReadOnlyList<HolidayPayRule> rules)
    {
        foreach (var detail in details.Where(d => d.Type == "WORK"))
        {
            if (!detail.WorkFrom.HasValue || !detail.WorkTo.HasValue || detail.WorkTo < detail.WorkFrom)
                throw new InvalidOperationException("Work detail requires valid work dates to verify holiday multipliers.");
            var matching = holidays.Where(h => h.IsActive && h.HolidayDate >= detail.WorkFrom && h.HolidayDate <= detail.WorkTo).ToList();
            if (matching.Count == 0)
            {
                if (detail.PublicHolidayId != null || detail.HolidayPayRuleId != null)
                    throw new InvalidOperationException("Holiday pay detail no longer matches the holiday schedule. Recalculation required.");
                continue;
            }
            if (matching.Count != 1 || detail.WorkFrom != matching[0].HolidayDate || detail.WorkTo != matching[0].HolidayDate)
                throw new InvalidOperationException("Work details must be split per holiday before submitting for approval.");
            var expected = Resolve(rules, matching[0].HolidayDate)
                ?? throw new InvalidOperationException($"No holiday pay rule found for date {matching[0].HolidayDate:dd/MM/yyyy}.");
            if (detail.PublicHolidayId != matching[0].PublicHolidayId || detail.HolidayPayRuleId != expected.HolidayPayRuleId
                || detail.Multiplier != expected.PayMultiplier || detail.Quantity is null or < 0 || detail.Rate is null or < 0)
                throw new InvalidOperationException("Payroll detail does not correctly record holiday, rule, and multiplier. Recalculation required before approval.");
        }
    }
}
