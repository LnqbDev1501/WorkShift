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
                throw new InvalidOperationException("Dòng công cần có ngày làm việc để kiểm tra hệ số ngày lễ.");
            var matching = holidays.Where(h => h.IsActive && h.HolidayDate >= detail.WorkFrom && h.HolidayDate <= detail.WorkTo).ToList();
            if (matching.Count == 0)
            {
                if (detail.PublicHolidayId != null || detail.HolidayPayRuleId != null)
                    throw new InvalidOperationException("Dòng lương ngày lễ không còn khớp lịch. Cần tính lại.");
                continue;
            }
            if (matching.Count != 1 || detail.WorkFrom != matching[0].HolidayDate || detail.WorkTo != matching[0].HolidayDate)
                throw new InvalidOperationException("Cần tách dòng công theo từng ngày lễ trước khi gửi duyệt.");
            var expected = Resolve(rules, matching[0].HolidayDate)
                ?? throw new InvalidOperationException($"Chưa có quy định hệ số cho ngày {matching[0].HolidayDate:dd/MM/yyyy}.");
            if (detail.PublicHolidayId != matching[0].PublicHolidayId || detail.HolidayPayRuleId != expected.HolidayPayRuleId
                || detail.Multiplier != expected.PayMultiplier || detail.Quantity is null or < 0 || detail.Rate is null or < 0)
                throw new InvalidOperationException("Dòng lương chưa lưu đúng ngày lễ, quy định và hệ số. Cần tính lại trước khi duyệt.");
        }
    }
}

