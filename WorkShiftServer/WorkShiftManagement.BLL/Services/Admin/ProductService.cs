using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class ProductService : IProductService
{
    private readonly IProductRepo _productRepo;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly AppDbContext _context;

    public ProductService(IProductRepo productRepo, ICloudinaryService cloudinaryService, AppDbContext context)
    {
        _productRepo = productRepo;
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    public async Task<PagedResponse<ProductResponse>> GetPagedAsync(ProductListQuery query, Guid actorId)
    {
        if (query == null)
            throw new ArgumentException("Query is required.");

        var actor = await GetActorAsync(actorId);

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

        int? categoryId = query.CategoryId is > 0 ? query.CategoryId : null;

        // Managers only see products of their own branch.
        int? branchId = actor.Role == "MANAGER" ? actor.BranchId : query.BranchId is > 0 ? query.BranchId : null;

        var (items, totalCount) = await _productRepo.GetPagedAsync(query.Search, branchId, categoryId, status, pageNumber, pageSize);

        return new PagedResponse<ProductResponse>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    public async Task<ProductDetailResponse?> GetByIdAsync(int id, Guid actorId)
    {
        if (id <= 0)
            return null;

        var actor = await GetActorAsync(actorId);

        var data = await _productRepo.GetByIdAsync(id);
        if (data == null)
            return null;

        if (actor.Role == "MANAGER" && data.BranchId != actor.BranchId)
            return null;

        return new ProductDetailResponse
        {
            ProductId = data.ProductId,
            BranchId = data.BranchId,
            BranchName = data.BranchName,
            CategoryId = data.CategoryId,
            CategoryName = data.CategoryName,
            ProductCode = data.ProductCode,
            ProductName = data.ProductName,
            Price = data.Price,
            Description = data.Description,
            ImageUrl = data.ImageUrl,
            Status = data.Status,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt
        };
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        if (request == null)
            throw new ArgumentException("Request body cannot be null.");

        var code = ValidateCode(request.ProductCode);
        var (categoryId, name, price, description, imageUrl) = await ValidateFieldsAsync(
            request.CategoryId, request.ProductName, request.Price, request.Description, request.ImageUrl);

        if (await _productRepo.ExistsByCodeInBranchAsync(code, branchId))
            throw new InvalidOperationException("Product code already exists in this branch.");

        var resolvedImageUrl = await ResolveImageUrlAsync(code, request.ImageFile, imageUrl);

        var now = DateTime.UtcNow;
        var entity = new Product
        {
            BranchId = branchId,
            CategoryId = categoryId,
            ProductCode = code,
            ProductName = name,
            Price = price,
            Description = description,
            ImageUrl = resolvedImageUrl,
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _productRepo.CreateAsync(entity);
        return Map(created);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        if (id <= 0)
            throw new ArgumentException("Product id must be greater than 0.");
        if (request == null)
            throw new ArgumentException("Request body cannot be null.");

        var existing = await _productRepo.GetByIdAsync(id);
        if (existing == null || existing.BranchId != branchId)
            return null;

        var (categoryId, name, price, description, imageUrl) = await ValidateFieldsAsync(
            request.CategoryId, request.ProductName, request.Price, request.Description, request.ImageUrl);

        var resolvedImageUrl = await ResolveImageUrlAsync(existing.ProductCode, request.ImageFile, imageUrl);

        var updated = await _productRepo.UpdateAsync(id, categoryId, name, price, description, resolvedImageUrl);
        return updated == null ? null : Map(updated);
    }

    public async Task<bool> DeleteAsync(int id, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        if (id <= 0)
            throw new ArgumentException("Product id must be greater than 0.");

        var detail = await _productRepo.GetByIdAsync(id);
        if (detail == null || detail.BranchId != branchId)
            return false;
        if (detail.Status != "ACTIVE")
            throw new InvalidOperationException("Product is already inactive.");

        return await _productRepo.SoftDeleteAsync(id);
    }

    public async Task<bool> RestoreAsync(int id, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        if (id <= 0)
            throw new ArgumentException("Product id must be greater than 0.");

        var detail = await _productRepo.GetByIdAsync(id);
        if (detail == null || detail.BranchId != branchId)
            return false;
        if (detail.Status == "ACTIVE")
            throw new InvalidOperationException("Product is already active.");

        if (!await _productRepo.CategoryIsActiveAsync(detail.CategoryId))
            throw new InvalidOperationException("The product's category is inactive. Activate the category first.");

        return await _productRepo.RestoreAsync(id);
    }

    private async Task<(string Role, int? BranchId)> GetActorAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated user ID is invalid.");

        var actor = await _context.Users.AsNoTracking()
            .Where(user => user.UserId == actorId && user.Status == "ACTIVE" && user.DeletedAt == null)
            .Select(user => new { Role = user.Role.RoleName, user.BranchId })
            .SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Your WorkShift account is inactive or unavailable.");

        return (actor.Role.Trim().ToUpperInvariant(), actor.BranchId);
    }

    private async Task<int> RequireManagerBranchAsync(Guid actorId)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.Role != "MANAGER")
            throw new UnauthorizedAccessException("Only branch managers can manage products.");
        if (!actor.BranchId.HasValue)
            throw new UnauthorizedAccessException("The manager account is not assigned to a branch.");

        return actor.BranchId.Value;
    }

    private async Task<string?> ResolveImageUrlAsync(string productCode, IFormFile? imageFile, string? imageUrl)
    {
        if (imageFile != null && imageFile.Length > 0)
            return await _cloudinaryService.UploadProductImageAsync(productCode, imageFile);

        return imageUrl;
    }

    private static string ValidateCode(string? code)
    {
        var trimmed = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Product code is required.");
        if (trimmed.Length > 50)
            throw new ArgumentException("Product code cannot exceed 50 characters.");
        return trimmed;
    }

    private async Task<(int CategoryId, string Name, decimal Price, string? Description, string? ImageUrl)> ValidateFieldsAsync(
        int categoryId, string? name, decimal price, string? description, string? imageUrl)
    {
        if (categoryId <= 0)
            throw new ArgumentException("Category is required.");
        if (!await _productRepo.CategoryIsActiveAsync(categoryId))
            throw new ArgumentException("Category does not exist or is inactive.");

        var trimmedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("Product name is required.");
        if (trimmedName.Length > 150)
            throw new ArgumentException("Product name cannot exceed 150 characters.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");
        if (decimal.Round(price, 2) != price)
            throw new ArgumentException("Price cannot have more than 2 decimal places.");

        var trimmedDescription = description?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedDescription) && trimmedDescription.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.");

        var trimmedImageUrl = imageUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedImageUrl) && trimmedImageUrl.Length > 500)
            throw new ArgumentException("Image URL cannot exceed 500 characters.");

        return (
            categoryId,
            trimmedName,
            price,
            string.IsNullOrWhiteSpace(trimmedDescription) ? null : trimmedDescription,
            string.IsNullOrWhiteSpace(trimmedImageUrl) ? null : trimmedImageUrl);
    }

    private static ProductResponse Map(DAL.Models.Admin.ProductData data) => new()
    {
        ProductId = data.ProductId,
        BranchId = data.BranchId,
        BranchName = data.BranchName,
        CategoryId = data.CategoryId,
        CategoryName = data.CategoryName,
        ProductCode = data.ProductCode,
        ProductName = data.ProductName,
        Price = data.Price,
        Status = data.Status,
        CreatedAt = data.CreatedAt,
        UpdatedAt = data.UpdatedAt
    };
}
