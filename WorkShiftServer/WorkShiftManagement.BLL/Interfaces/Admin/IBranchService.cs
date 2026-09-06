using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IBranchService
{
    Task<PagedResponse<BranchResponse>> GetPagedAsync(BranchListQuery query);
    Task<BranchDetailResponse?> GetByIdAsync(int id);
    Task<BranchResponse> CreateAsync(CreateBranchRequest request);
    Task<BranchDetailResponse?> UpdateAsync(int id, UpdateBranchRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<BranchEmployeesResponse?> GetEmployeesAsync(int branchId, BranchEmployeeListQuery query);
}
