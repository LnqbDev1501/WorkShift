using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IUserService
{
    Task<CreateAdminUserResponse> CreateAdminUserAsync(CreateAdminUserRequest request);
}
