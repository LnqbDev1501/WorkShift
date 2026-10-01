using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerEmploymentContractService(IEmploymentContractRepo repository) : IManagerEmploymentContractService
{
    public async Task<CreateEmploymentContractResponse> CreateAsync(Guid managerId,
        CreateEmploymentContractRequest request, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        if (request.StaffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId is required.");
        if (request.StartDate == default || request.EndDate <= request.StartDate)
            throw new ArgumentException("StartDate is required and EndDate must be later than StartDate.");
        if (string.IsNullOrWhiteSpace(request.EmployeePosition) || request.EmployeePosition.Length > 100)
            throw new ArgumentException("EmployeePosition is required and must not exceed 100 characters.");

        var staff = await repository.GetCreationUserAsync(request.StaffUserId, cancellationToken);
        if (staff == null || staff.DeletedAt != null)
            throw new KeyNotFoundException("Staff does not exist.");
        if (!string.Equals(staff.Role?.RoleName, "STAFF", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The selected user must have the STAFF role.");
        if (staff.BranchId != manager.BranchId)
            throw new UnauthorizedAccessException("Staff does not belong to the Manager's branch.");
        if (staff.Status != "PENDING" && staff.Status != "ACTIVE")
            throw new ArgumentException("Staff must be PENDING or ACTIVE.");
        var branch = staff.Branch;
        if (branch == null || branch.Status != "ACTIVE")
            throw new ArgumentException("Staff branch must be ACTIVE.");
        var region = branch.Region;
        if (region == null || !region.IsActive)
            throw new ArgumentException("Branch region must be active.");
        var wage = region.RegionalMinimumWage;
        if (region.RegionalMinimumWageId == null || wage == null || wage.RegionalMinimumWageId != region.RegionalMinimumWageId)
            throw new ArgumentException("Region has no assigned minimum wage configuration.");
        // Activation closes the old wage on the day before its replacement: both bounds are inclusive.
        if (wage.Status != "ACTIVE" || wage.MinimumHourlyRate <= 0 || wage.EffectiveFrom == null ||
            request.StartDate < wage.EffectiveFrom || request.StartDate > wage.EffectiveTo)
            throw new ArgumentException("Regional minimum wage must be ACTIVE and effective on the contract start date.");
        // Existing database index uq_one_pending_contract_per_staff also enforces this under concurrency.
        if (await repository.HasPendingContractAsync(staff.UserId, cancellationToken))
            throw new InvalidOperationException("Staff already has a draft or pending employment contract.");

        var now = DateTime.UtcNow;
        var contract = new EmploymentContract
        {
            UserId = staff.UserId,
            ContractNumber = $"EC-{Guid.NewGuid():N}",
            StartDate = request.StartDate, EndDate = request.EndDate,
            HourlyRate = wage.MinimumHourlyRate,
            EmployeePositionSnapshot = request.EmployeePosition.Trim(),
            BranchId = branch.BranchId, RegionId = region.RegionId,
            EmployeeFullNameSnapshot = staff.FullName, EmployeePhoneSnapshot = staff.Phone,
            EmployeeAddressSnapshot = staff.Address, EmployeeIdentityNoSnapshot = staff.IdentityNo,
            BranchNameSnapshot = branch.BranchName, BranchAddressSnapshot = branch.Address,
            RegionNameSnapshot = region.RegionName, ContractTerms = request.ContractTerms,
            Status = "DRAFT", CreatedBy = managerId, CreatedAt = now, UpdatedAt = now
        };
        await repository.CreateDraftAsync(contract, cancellationToken);
        return new(contract.ContractId, contract.ContractNumber, contract.UserId, contract.EmployeeFullNameSnapshot,
            contract.BranchId, contract.BranchNameSnapshot, contract.RegionId, contract.RegionNameSnapshot,
            contract.StartDate, contract.EndDate, contract.HourlyRate, contract.EmployeePositionSnapshot,
            contract.ContractTerms, contract.Status, contract.CreatedBy, contract.CreatedAt);
    }
    public async Task<IReadOnlyList<ContractStaffOptionResponse>> GetStaffOptionsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        var manager = await RequireManagerAsync(managerId, cancellationToken);
        var staff = await repository.GetEligibleStaffAsync(manager.BranchId!.Value, cancellationToken);
        return staff.Select(u => new ContractStaffOptionResponse(u.UserId, u.FullName)).ToList();
    }

    private async Task<User> RequireManagerAsync(Guid managerId, CancellationToken cancellationToken)
    {
        var manager = await repository.GetCreationUserAsync(managerId, cancellationToken);
        if (manager == null || manager.DeletedAt != null || manager.Status != "ACTIVE" ||
            !string.Equals(manager.Role?.RoleName, "MANAGER", StringComparison.OrdinalIgnoreCase) ||
            manager.Role?.Status != "ACTIVE" || manager.BranchId == null)
            throw new UnauthorizedAccessException("An active Manager with an assigned branch is required.");

        return manager;
    }
}
