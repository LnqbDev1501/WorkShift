using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetPagedAsync(CategoryListQuery query, Guid? actorId = null);
    Task<CategoryDetailResponse?> GetByIdAsync(int id, Guid? actorId = null);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);
    Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
}
