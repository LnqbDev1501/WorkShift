using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IUserService
{
    Task<AdminUserListResponse> GetAdminUsersAsync(GetAdminUsersRequest request);
    Task<CreateAdminUserResponse> CreateAdminUserAsync(CreateAdminUserRequest request);
    Task ResendSetupLinkAsync(Guid userId);
    Task ActivateUserAsync(Guid userId);
}
