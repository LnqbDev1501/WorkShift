using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerStaffService
{
    Task<StaffAccountPagedResponse> GetStaffListAsync(ManagerStaffListQuery query, Guid managerId);
    Task<StaffAccountDetailResponse?> GetStaffDetailAsync(Guid staffId, Guid managerId);
    Task<StaffAccountDetailResponse> CreateStaffAsync(CreateStaffAccountRequest request, Guid managerId);
    Task<StaffAccountDetailResponse?> UpdateStaffAsync(Guid staffId, UpdateStaffAccountRequest request, Guid managerId);
    Task<bool> DeactivateStaffAsync(Guid staffId, Guid managerId);
    Task<bool> RestoreStaffAsync(Guid staffId, Guid managerId, int? branchId);
    Task<bool> ResendStaffSetupLinkAsync(Guid staffId, Guid managerId);
}
