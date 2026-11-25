using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Policies.Requests;

// Pure validation of the existing request/detail tables; no salary history writes.
public static class SalaryIncreaseRequestPolicy
{
    public static void ValidateSubmissionRule(RequestLimitRule rule, DateOnly today)
    {
        if (rule.RequestType != "SALARY_INCREASE" || rule.MinimumAdvanceDays != 0 || rule.MaxRequestsPerMonth <= 0)
            throw new InvalidOperationException("Salary increase rule requires MinimumAdvanceDays = 0 and positive monthly limit; effective date is proposed later by Manager/Admin.");
        if (rule.SubmissionDeadlineDayOfWeek is < 1 or > 7)
            throw new InvalidOperationException("Submission deadline day of week must be between 1 (Monday) and 7 (Sunday).");
        // A configured cutoff limits submission within the current VN calendar week;
        // it is unrelated to the later proposal's salary effective date.
        var weekday = ((int)today.DayOfWeek + 6) % 7 + 1;
        if (rule.SubmissionDeadlineDayOfWeek is { } cutoff && weekday > cutoff)
            throw new InvalidOperationException("The submission deadline for the current week has passed. Please submit starting from next week.");
    }

    public static void ValidateRate(decimal current, decimal proposed)
    {
        if (current <= 0 || proposed <= current || proposed > 9999999999999999.99m
            || proposed != decimal.Truncate(proposed * 100) / 100)
            throw new InvalidOperationException("Proposed rate must be higher than current rate and fit NUMERIC(18,2); no rounding permitted.");
    }

    public static void ValidateSource(User staff, int branchId, StaffSalaryHistory salary, decimal proposed, DateOnly today)
    {
        if (staff.DeletedAt != null || staff.Status != "ACTIVE" || staff.Role?.RoleName != "STAFF" || staff.Role.Status != "ACTIVE"
            || staff.BranchId != branchId || staff.Branch?.Status != "ACTIVE")
            throw new UnauthorizedAccessException("Staff member must be active in their assigned branch.");
        var contract = salary.Contract;
        if (salary.UserId != staff.UserId || salary.Status is not ("ACTIVE" or "INACTIVE" or "SCHEDULED")
            || salary.EffectiveFrom > today || salary.EffectiveTo.HasValue && salary.EffectiveTo < today
            || contract == null || salary.ContractId <= 0 || salary.ContractId != contract.ContractId
            || contract.UserId != staff.UserId || contract.BranchId != branchId
            || contract.Status != "ACTIVE" || contract.ApprovedAt == null || contract.StaffAcceptedAt == null
            || contract.StartDate > today || contract.EndDate.HasValue && contract.EndDate < today)
            throw new InvalidOperationException("An approved and active employment contract accepted by staff with an active salary rate is required.");
        ValidateRate(salary.HourlyRate, proposed);
    }

    public static void ValidateManagerApproval(EmployeeRequest request, DateOnly today,
        IReadOnlyCollection<StaffSalaryHistory> salaries)
    {
        if (request.RequestType != "SALARY_INCREASE" || request.Status != "PENDING")
            throw new InvalidOperationException("Only PENDING salary increase requests can be reviewed.");
        var d = request.SalaryIncreaseRequestDetail;
        if (d == null || d.RequestId != request.RequestId || d.CurrentContract == null
            || d.CurrentContractId != d.CurrentContract.ContractId || d.CurrentContract.UserId != request.UserId
            || d.CurrentContract.BranchId != request.BranchId || d.CurrentContract.Status != "ACTIVE"
            || d.CurrentContract.ApprovedAt == null || d.CurrentContract.StaffAcceptedAt == null
            || d.CurrentContract.StartDate > today || d.CurrentContract.EndDate.HasValue && d.CurrentContract.EndDate < today
            || request.User.DeletedAt != null || request.User.Status != "ACTIVE" || request.User.Role?.RoleName != "STAFF"
            || request.User.Role.Status != "ACTIVE" || request.User.BranchId != request.BranchId || request.Branch.Status != "ACTIVE")
            throw new InvalidOperationException("Contract, staff member, or salary increase request detail is invalid.");
        ValidateRate(d.CurrentHourlyRate, d.ProposedHourlyRate);
        var current = salaries.Where(s => s.UserId == request.UserId
            && s.Status is "ACTIVE" or "INACTIVE" or "SCHEDULED"
            && s.EffectiveFrom <= today && (!s.EffectiveTo.HasValue || s.EffectiveTo >= today)).ToList();
        if (current.Count != 1)
            throw new InvalidOperationException("Exactly one current active salary rate is required before reviewing salary increase request.");
        if (current[0].ContractId != d.CurrentContractId || current[0].HourlyRate != d.CurrentHourlyRate)
            throw new InvalidOperationException("Contract or salary rate has changed since submission. Cannot approve based on stale source; staff must submit a new request.");
        ValidateSource(request.User, request.BranchId, current[0], d.ProposedHourlyRate, today);
    }
}
