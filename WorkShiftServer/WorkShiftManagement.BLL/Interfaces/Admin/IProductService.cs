using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetPagedAsync(ProductListQuery query, Guid actorId);
    Task<ProductDetailResponse?> GetByIdAsync(int id, Guid actorId);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid actorId);
    Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, Guid actorId);
    Task<bool> DeleteAsync(int id, Guid actorId);
    Task<bool> RestoreAsync(int id, Guid actorId);
}
