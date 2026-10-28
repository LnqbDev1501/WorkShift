using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public partial class StaffEmployeeRequestRepo
{
    public async Task<EmployeeRequest> SubmitSalaryIncreaseRequestAsync(Guid staffId, int branchId, decimal proposedHourlyRate,
        string reason, DateOnly todayVn, CancellationToken cancellationToken = default)
    {
        if (staffId == Guid.Empty || branchId <= 0 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("Thông tin yêu cầu tăng lương không hợp lệ.");
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        // Same per-Staff serialization as the existing requests; prevents duplicate submissions/quota races.
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT user_id FROM public.users WHERE user_id = {staffId} FOR UPDATE", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("LOCK TABLE public.staff_salary_history, public.employment_contracts, public.request_limit_rules IN SHARE MODE", cancellationToken);
        var staff = await GetActiveStaffAsync(staffId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Tài khoản Staff không hoạt động.");
        var rule = await GetActiveRuleAsync(branchId, "SALARY_INCREASE", todayVn, cancellationToken)
            ?? throw new InvalidOperationException("Chi nhánh chưa có Request Limit Rule SALARY_INCREASE đang ACTIVE và có hiệu lực.");
        // Retain the existing fail-closed deadline rule. Staff requests have no effective date yet;
        // the future salary effective date belongs to the later Manager/Admin proposal workflow.
        ValidateRuleSupported(rule);
        if (rule.MinimumAdvanceDays != 0 || rule.MaxRequestsPerMonth <= 0)
            throw new InvalidOperationException("Bước Staff xin tăng lương chưa chọn ngày áp dụng; rule cần MinimumAdvanceDays = 0 và giới hạn tháng lớn hơn 0.");
        await ValidateMonthlyLimitAsync(staffId, "SALARY_INCREASE", rule.MaxRequestsPerMonth, todayVn, cancellationToken);
        if (await _context.EmployeeRequests.AnyAsync(r => r.UserId == staffId && r.RequestType == "SALARY_INCREASE" && r.Status == "PENDING", cancellationToken))
            throw new InvalidOperationException("Bạn đang có yêu cầu tăng lương PENDING; chờ Manager xử lý hoặc hủy trước khi gửi yêu cầu mới.");
        var salaries = await _context.StaffSalaryHistories.AsNoTracking().Include(s => s.Contract)
            .Where(s => s.UserId == staffId && (s.Status == "ACTIVE" || s.Status == "INACTIVE" || s.Status == "SCHEDULED")
                && s.EffectiveFrom <= todayVn && (!s.EffectiveTo.HasValue || s.EffectiveTo >= todayVn))
            .ToListAsync(cancellationToken);
        if (salaries.Count != 1) throw new InvalidOperationException("Cần đúng một bản ghi mức lương có hiệu lực hôm nay; dữ liệu đang thiếu hoặc chồng lấn.");
        var salary = salaries[0];
        SalaryIncreaseRequestPolicy.ValidateSource(staff, branchId, salary, proposedHourlyRate, todayVn);
        var now = DateTime.UtcNow;
        var request = new EmployeeRequest { UserId = staffId, BranchId = branchId, RequestType = "SALARY_INCREASE",
            Reason = reason.Trim(), Status = "PENDING", AppliedRequestLimitRuleId = rule.RequestLimitRuleId, CreatedAt = now, UpdatedAt = now,
            SalaryIncreaseRequestDetail = new SalaryIncreaseRequestDetail { CurrentContractId = salary.ContractId,
                CurrentHourlyRate = salary.HourlyRate, ProposedHourlyRate = proposedHourlyRate } };
        _context.EmployeeRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        _context.AuditLogs.Add(new AuditLog { UserId = staffId, Action = "CREATE", TableName = "employee_requests",
            RecordId = request.RequestId.ToString(), CreatedAt = now,
            NewValue = JsonSerializer.Serialize(new { request.RequestId, request.UserId, request.BranchId, request.RequestType,
                request.Reason, request.Status, request.AppliedRequestLimitRuleId, salary.SalaryHistoryId, salary.ContractId,
                currentHourlyRate = salary.HourlyRate, proposedHourlyRate }) });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request;
    }
}
