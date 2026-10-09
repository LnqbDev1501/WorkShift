using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IProductRepo
{
    Task<(List<ProductData> Items, int TotalCount)> GetPagedAsync(string? search, int? branchId, int? categoryId, string? status, int pageNumber, int pageSize);
    Task<ProductDetailData?> GetByIdAsync(int id);
    Task<bool> ExistsByCodeInBranchAsync(string productCode, int branchId);
    Task<bool> ExistsByCodeExceptIdInBranchAsync(string productCode, int branchId, int excludeProductId);
    Task<bool> CategoryIsActiveAsync(int categoryId);
    Task<ProductData> CreateAsync(Product product);
    Task<ProductData?> UpdateAsync(int id, int categoryId, string productName, decimal price, string? description, string? imageUrl);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
}
