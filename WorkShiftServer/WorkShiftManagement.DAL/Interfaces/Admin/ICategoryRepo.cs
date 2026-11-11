using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface ICategoryRepo
{
    Task<(List<CategoryData> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int pageNumber, int pageSize, int? branchId = null);
    Task<CategoryDetailData?> GetByIdAsync(int id, int? branchId = null);
    Task<bool> ExistsByNameAsync(string categoryName);
    Task<bool> ExistsByNameExceptIdAsync(string categoryName, int excludeCategoryId);
    Task<bool> HasActiveProductsAsync(int categoryId);
    Task<CategoryData> CreateAsync(ProductCategory category);
    Task<CategoryData?> UpdateAsync(int id, string categoryName, string? description);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
}
