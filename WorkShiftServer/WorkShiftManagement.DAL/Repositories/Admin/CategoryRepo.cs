using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class CategoryRepo : ICategoryRepo
{
    private readonly AppDbContext _context;

    public CategoryRepo(AppDbContext context) => _context = context;

    public async Task<(List<CategoryData> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int pageNumber, int pageSize, int? branchId = null)
    {
        var query = _context.ProductCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(category => EF.Functions.ILike(category.CategoryName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(category => category.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(category => category.CategoryId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new CategoryData
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                Status = category.Status,
                ProductCount = category.Products.Count(p => !branchId.HasValue || p.BranchId == branchId.Value),
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<CategoryDetailData?> GetByIdAsync(int id, int? branchId = null)
    {
        if (id <= 0)
            return Task.FromResult<CategoryDetailData?>(null);

        return _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.CategoryId == id)
            .Select(category => new CategoryDetailData
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                Status = category.Status,
                ProductCount = category.Products.Count(p => !branchId.HasValue || p.BranchId == branchId.Value),
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                Products = category.Products
                    .Where(product => !branchId.HasValue || product.BranchId == branchId.Value)
                    .OrderBy(product => product.ProductName)
                    .Select(product => new CategoryProductData
                    {
                        ProductId = product.ProductId,
                        BranchId = product.BranchId,
                        BranchName = product.Branch.BranchName,
                        ProductCode = product.ProductCode,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        ImageUrl = product.ImageUrl,
                        Status = product.Status
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public Task<bool> ExistsByNameAsync(string categoryName)
    {
        return _context.ProductCategories
            .AsNoTracking()
            .AnyAsync(category => EF.Functions.ILike(category.CategoryName, categoryName.Trim()));
    }

    public Task<bool> ExistsByNameExceptIdAsync(string categoryName, int excludeCategoryId)
    {
        var trimmed = categoryName.Trim();
        return _context.ProductCategories
            .AsNoTracking()
            .AnyAsync(category =>
                category.CategoryId != excludeCategoryId &&
                EF.Functions.ILike(category.CategoryName, trimmed));
    }

    public Task<bool> HasActiveProductsAsync(int categoryId)
    {
        return _context.Products
            .AsNoTracking()
            .AnyAsync(product => product.CategoryId == categoryId && product.Status == "ACTIVE");
    }

    public async Task<CategoryData> CreateAsync(ProductCategory category)
    {
        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryData
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Status = category.Status,
            ProductCount = 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<CategoryData?> UpdateAsync(int id, string categoryName, string? description)
    {
        var category = await _context.ProductCategories.FirstOrDefaultAsync(item => item.CategoryId == id);
        if (category == null)
            return null;

        category.CategoryName = categoryName;
        category.Description = description;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var productCount = await _context.Products
            .AsNoTracking()
            .CountAsync(product => product.CategoryId == id);

        return new CategoryData
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Status = category.Status,
            ProductCount = productCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var category = await _context.ProductCategories.FirstOrDefaultAsync(item => item.CategoryId == id);
        if (category == null || category.Status != "ACTIVE")
            return false;

        category.Status = "INACTIVE";
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var category = await _context.ProductCategories.FirstOrDefaultAsync(item => item.CategoryId == id);
        if (category == null || category.Status == "ACTIVE")
            return false;

        category.Status = "ACTIVE";
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
