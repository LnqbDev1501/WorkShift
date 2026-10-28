using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Staff;

// Pure validation of the existing request/detail tables; no salary history writes.
public static class SalaryIncreaseRequestPolicy
{
    public static void ValidateRate(decimal current, decimal proposed)
    {
        if (current <= 0 || proposed <= current || proposed > 9999999999999999.99m
            || proposed != decimal.Truncate(proposed * 100) / 100)
            throw new InvalidOperationException("Mức đề xuất phải cao hơn mức đang hưởng và phù hợp NUMERIC(18,2); không tự làm tròn.");
    }
    public static void ValidateSource(User staff, int branchId, StaffSalaryHistory salary, decimal proposed, DateOnly today)
    {
        if (staff.DeletedAt != null || staff.Status != "ACTIVE" || staff.Role?.RoleName != "STAFF" || staff.Role.Status != "ACTIVE"
            || staff.BranchId != branchId || staff.Branch?.Status != "ACTIVE")
            throw new UnauthorizedAccessException("Staff phải đang hoạt động tại chi nhánh được phân công.");
        var contract = salary.Contract;
        if (salary.UserId != staff.UserId || salary.Status is not ("ACTIVE" or "INACTIVE" or "SCHEDULED")
            || salary.EffectiveFrom > today || salary.EffectiveTo.HasValue && salary.EffectiveTo < today
            || contract == null || salary.ContractId <= 0 || salary.ContractId != contract.ContractId
            || contract.UserId != staff.UserId || contract.BranchId != branchId
            || contract.Status != "ACTIVE" || contract.ApprovedAt == null || contract.StaffAcceptedAt == null
            || contract.StartDate > today || contract.EndDate.HasValue && contract.EndDate < today)
            throw new InvalidOperationException("Cần mức lương và hợp đồng đã duyệt, được Staff chấp nhận, đang có hiệu lực để xin tăng lương.");
        ValidateRate(salary.HourlyRate, proposed);
    }
    public static void ValidateManagerApproval(EmployeeRequest request, DateOnly today)
    {
        if (request.RequestType != "SALARY_INCREASE" || request.Status != "PENDING")
            throw new InvalidOperationException("Chỉ duyệt yêu cầu tăng lương đang PENDING.");
        var d = request.SalaryIncreaseRequestDetail;
        if (d == null || d.RequestId != request.RequestId || d.CurrentContract == null
            || d.CurrentContractId != d.CurrentContract.ContractId || d.CurrentContract.UserId != request.UserId
            || d.CurrentContract.BranchId != request.BranchId || d.CurrentContract.Status != "ACTIVE"
            || d.CurrentContract.ApprovedAt == null || d.CurrentContract.StaffAcceptedAt == null
            || d.CurrentContract.StartDate > today || d.CurrentContract.EndDate.HasValue && d.CurrentContract.EndDate < today
            || request.User.DeletedAt != null || request.User.Status != "ACTIVE" || request.User.Role?.RoleName != "STAFF"
            || request.User.Role.Status != "ACTIVE" || request.User.BranchId != request.BranchId || request.Branch.Status != "ACTIVE")
            throw new InvalidOperationException("Nguồn hợp đồng/nhân viên hoặc chi tiết yêu cầu tăng lương không hợp lệ.");
        ValidateRate(d.CurrentHourlyRate, d.ProposedHourlyRate);
    }
}
