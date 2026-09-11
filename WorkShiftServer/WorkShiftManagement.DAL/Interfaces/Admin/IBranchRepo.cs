using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IBranchRepo
{
    Task<(List<BranchData> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int? regionId,
        string? status,
        int pageNumber,
        int pageSize);

    Task<BranchDetailData?> GetByIdAsync(int id);
    Task<bool> ExistsByCodeAsync(string branchCode);
    Task<bool> ExistsByNameAsync(string branchName);
    Task<bool> ExistsByNameExceptIdAsync(string branchName, int excludeBranchId);

    Task<BranchData> CreateAsync(Branch branch);
    Task<BranchDetailData?> UpdateAsync(Branch branch);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);

    Task<(List<BranchEmployeeData> Items, int TotalCount)> GetEmployeesPagedAsync(
        int branchId,
        string? search,
        string? status,
        string? role,
        int pageNumber,
        int pageSize);

    Task<List<Branch>> GetAssignableBranchesAsync();

    Task<Branch?> GetEntityByIdAsync(int id);
}
