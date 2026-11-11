using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepo _categoryRepo;
    private readonly AppDbContext _context;

    public CategoryService(ICategoryRepo categoryRepo, AppDbContext context)
    {
        _categoryRepo = categoryRepo;
        _context = context;
    }

    private async Task<(string? Role, int? BranchId)> GetActorAsync(Guid? actorId)
    {
        if (!actorId.HasValue || actorId.Value == Guid.Empty)
            return (null, null);

        var actor = await _context.Users.AsNoTracking()
            .Where(user => user.UserId == actorId.Value && user.Status == "ACTIVE" && user.DeletedAt == null)
            .Select(user => new { Role = user.Role.RoleName, user.BranchId })
            .SingleOrDefaultAsync();

        return actor == null ? (null, null) : (actor.Role.Trim().ToUpperInvariant(), actor.BranchId);
    }

    public async Task<PagedResponse<CategoryResponse>> GetPagedAsync(CategoryListQuery query, Guid? actorId = null)
    {
        if (query == null)
            throw new ArgumentException("Query is required.");

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
            pageSize = 100;

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            status = query.Status.Trim().ToUpperInvariant();
            if (status != "ACTIVE" && status != "INACTIVE")
                throw new ArgumentException("Status filter only accepts 'ACTIVE' or 'INACTIVE'.");
        }

        int? branchId = null;
        if (actorId.HasValue)
        {
            var (role, userBranchId) = await GetActorAsync(actorId);
            if (role is "MANAGER" or "STAFF" or "ACCOUNTANT")
                branchId = userBranchId.HasValue ? userBranchId.Value : -1;
            else if (query.BranchId is > 0)
                branchId = query.BranchId;
        }
        else if (query.BranchId is > 0)
        {
            branchId = query.BranchId;
        }

        var (items, totalCount) = await _categoryRepo.GetPagedAsync(query.Search, status, pageNumber, pageSize, branchId);

        return new PagedResponse<CategoryResponse>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    public async Task<CategoryDetailResponse?> GetByIdAsync(int id, Guid? actorId = null)
    {
        if (id <= 0)
            return null;

        int? branchId = null;
        if (actorId.HasValue)
        {
            var (role, userBranchId) = await GetActorAsync(actorId);
            if (role is "MANAGER" or "STAFF" or "ACCOUNTANT")
                branchId = userBranchId.HasValue ? userBranchId.Value : -1;
        }

        var data = await _categoryRepo.GetByIdAsync(id, branchId);
        if (data == null)
            return null;

        return new CategoryDetailResponse
        {
            CategoryId = data.CategoryId,
            CategoryName = data.CategoryName,
            Description = data.Description,
            Status = data.Status,
            ProductCount = data.ProductCount,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            Products = data.Products.Select(product => new CategoryProductResponse
            {
                ProductId = product.ProductId,
                BranchId = product.BranchId,
                BranchName = product.BranchName,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Price = product.Price,
                Status = product.Status
            }).ToList()
        };
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentException("Request body cannot be null.");

        var (name, description) = ValidateFields(request.CategoryName, request.Description);

        if (await _categoryRepo.ExistsByNameAsync(name))
            throw new InvalidOperationException("Category name already exists.");

        var now = DateTime.UtcNow;
        var entity = new ProductCategory
        {
            CategoryName = name,
            Description = description,
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _categoryRepo.CreateAsync(entity);
        return Map(created);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        if (id <= 0)
            throw new ArgumentException("Category id must be greater than 0.");
        if (request == null)
            throw new ArgumentException("Request body cannot be null.");

        var (name, description) = ValidateFields(request.CategoryName, request.Description);

        if (await _categoryRepo.ExistsByNameExceptIdAsync(name, id))
            throw new InvalidOperationException("Category name already exists.");

        var updated = await _categoryRepo.UpdateAsync(id, name, description);
        return updated == null ? null : Map(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Category id must be greater than 0.");

        var detail = await _categoryRepo.GetByIdAsync(id);
        if (detail == null)
            return false;
        if (detail.Status != "ACTIVE")
            throw new InvalidOperationException("Category is already inactive.");
        if (await _categoryRepo.HasActiveProductsAsync(id))
            throw new InvalidOperationException("Category has active products and cannot be deactivated.");

        return await _categoryRepo.SoftDeleteAsync(id);
    }

    public async Task<bool> RestoreAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Category id must be greater than 0.");

        var detail = await _categoryRepo.GetByIdAsync(id);
        if (detail == null)
            return false;
        if (detail.Status == "ACTIVE")
            throw new InvalidOperationException("Category is already active.");

        return await _categoryRepo.RestoreAsync(id);
    }

    private static (string Name, string? Description) ValidateFields(string? name, string? description)
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("Category name is required.");
        if (trimmedName.Length > 150)
            throw new ArgumentException("Category name cannot exceed 150 characters.");

        var trimmedDescription = description?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedDescription) && trimmedDescription.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.");

        return (trimmedName, string.IsNullOrWhiteSpace(trimmedDescription) ? null : trimmedDescription);
    }

    private static CategoryResponse Map(DAL.Models.Admin.CategoryData data) => new()
    {
        CategoryId = data.CategoryId,
        CategoryName = data.CategoryName,
        Description = data.Description,
        Status = data.Status,
        ProductCount = data.ProductCount,
        CreatedAt = data.CreatedAt,
        UpdatedAt = data.UpdatedAt
    };
}
