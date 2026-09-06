using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IUserService
{
    Task<AdminUserListResponse> GetAdminUsersAsync(GetAdminUsersRequest request);
    Task<AdminUserDetailResponse> GetAdminUserDetailAsync(Guid userId);
    Task<CreateAdminUserResponse> CreateAdminUserAsync(CreateAdminUserRequest request);
    Task ResendSetupLinkAsync(Guid userId);
    Task SoftDeleteAdminUserAsync(Guid userId);
    Task RestoreAdminUserAsync(Guid userId, RestoreAdminUserRequest request);
    Task ActivateUserAsync(Guid userId);
}
