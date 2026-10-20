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
        if (value == null || value.Length > 1000) throw new ArgumentException("Cần nội dung không rỗng, tối đa 1000 ký tự.");
        return value;
    }
    public static void Rate(decimal proposed)
    {
        if (proposed <= 0 || proposed > 9999999999999999.99m || proposed != decimal.Truncate(proposed * 100m) / 100m)
            throw new ArgumentException("Mức lương phải dương, tối đa 16 chữ số nguyên và 2 chữ số thập phân; không tự làm tròn.");
    }
    public static void ProposedDate(DateOnly date, DateOnly today)
    {
        if (date == DateOnly.MaxValue || date.DayOfWeek != DayOfWeek.Monday || date < NextMonday(today))
            throw new ArgumentException("Ngày áp dụng phải là thứ Hai từ tuần tiếp theo trở đi (UTC+7).");
    }
    public static DateOnly ApprovalDate(SalaryChangeRequest item, DateOnly today, DateOnly? selected, string? changeReason)
    {
        if (item.ProposedEffectiveFrom > today)
        {
            if (selected.HasValue && selected != item.ProposedEffectiveFrom || Text(changeReason) != null)
                throw new ArgumentException("Ngày đề xuất chưa đến; phải giữ nguyên ngày và không nhập lý do đổi ngày.");
            ProposedDate(item.ProposedEffectiveFrom, today);
            return item.ProposedEffectiveFrom;
        }
        if (!selected.HasValue) throw new ArgumentException("Ngày đề xuất đã đến hoặc đã qua; Admin phải chọn ngày mới.");
        RequiredText(changeReason);
        ProposedDate(selected.Value, today);
        return selected.Value;
    }
    public static StaffSalaryHistory Source(User staff, int branchId, IReadOnlyCollection<StaffSalaryHistory> salaries,
        decimal proposed, DateOnly today, DateOnly effective, int? expectedId = null)
    {
        if (staff.Status != "ACTIVE" || staff.DeletedAt != null || staff.Role?.RoleName != "STAFF"
            || staff.Role.Status != "ACTIVE" || staff.BranchId != branchId || staff.Branch?.Status != "ACTIVE")
            throw new InvalidOperationException("Staff/chi nhánh phải đang hoạt động và thuộc đúng phạm vi đề xuất.");
        var rows = salaries.Where(s => s.UserId == staff.UserId && s.Status is "ACTIVE" or "INACTIVE" or "SCHEDULED").ToList();
        var current = rows.Where(s => s.EffectiveFrom <= today && (!s.EffectiveTo.HasValue || s.EffectiveTo >= today)).ToList();
        if (current.Count != 1) throw new InvalidOperationException("Thiếu mức lương hiện hành hoặc các khoảng lương đang chồng lấn.");
        var source = current[0];
        if (expectedId.HasValue && source.SalaryHistoryId != expectedId)
            throw new InvalidOperationException("Mức lương làm căn cứ đã thay đổi. Manager cần tạo đề xuất mới.");
        var contract = source.Contract;
        if (contract == null || contract.ContractId != source.ContractId || contract.UserId != staff.UserId
            || contract.BranchId != branchId || contract.Status != "ACTIVE" || contract.ApprovedAt == null
            || contract.StaffAcceptedAt == null || contract.StartDate > today
            || contract.EndDate.HasValue && contract.EndDate < effective
            || source.HourlyRate <= 0 || proposed <= source.HourlyRate)
            throw new InvalidOperationException("Mức mới phải cao hơn mức nguồn; hợp đồng đã duyệt/chấp nhận và còn hiệu lực đến ngày áp dụng.");
        if (rows.Any(s => s.SalaryHistoryId != source.SalaryHistoryId && s.EffectiveFrom > today
            && s.Contract.Status != "CANCELLED"))
            throw new InvalidOperationException("Đã có mức lương tương lai. Cần xử lý lịch đã có trước khi đề xuất/duyệt tiếp.");
        if (source.EffectiveTo.HasValue && source.EffectiveTo < effective)
            throw new InvalidOperationException("Mức nguồn kết thúc trước ngày áp dụng, không được tự kéo dài khoảng hiệu lực.");
        return source;
    }
}
