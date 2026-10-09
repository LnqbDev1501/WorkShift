using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IEmployeeTypeService
{
    Task<PagedResponse<EmployeeTypeResponse>> GetPagedAsync(EmployeeTypeListQuery query);
    Task<EmployeeTypeResponse?> GetByIdAsync(int id);
    Task<EmployeeTypeResponse> CreateAsync(CreateEmployeeTypeRequest request);
    Task<EmployeeTypeResponse?> UpdateAsync(int id, UpdateEmployeeTypeRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
}
