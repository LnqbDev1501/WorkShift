using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class ProductRepo : IProductRepo
{
    private readonly AppDbContext _context;

    public ProductRepo(AppDbContext context) => _context = context;

    public async Task<(List<ProductData> Items, int TotalCount)> GetPagedAsync(string? search, int? branchId, int? categoryId, string? status, int pageNumber, int pageSize)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.ProductCode, pattern) ||
                EF.Functions.ILike(product.ProductName, pattern));
        }

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(product => product.BranchId == branchId.Value);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(product => product.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(product => product.ProductId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductData
            {
                ProductId = product.ProductId,
                BranchId = product.BranchId,
                BranchName = product.Branch.BranchName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.CategoryName,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Price = product.Price,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<ProductDetailData?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult<ProductDetailData?>(null);

        return _context.Products
            .AsNoTracking()
            .Where(product => product.ProductId == id)
            .Select(product => new ProductDetailData
            {
                ProductId = product.ProductId,
                BranchId = product.BranchId,
                BranchName = product.Branch.BranchName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.CategoryName,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Price = product.Price,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public Task<bool> ExistsByCodeInBranchAsync(string productCode, int branchId)
    {
        var trimmed = productCode.Trim();
        return _context.Products
            .AsNoTracking()
            .AnyAsync(product =>
                product.BranchId == branchId &&
                EF.Functions.ILike(product.ProductCode, trimmed));
    }

    public Task<bool> ExistsByCodeExceptIdInBranchAsync(string productCode, int branchId, int excludeProductId)
    {
        var trimmed = productCode.Trim();
        return _context.Products
            .AsNoTracking()
            .AnyAsync(product =>
                product.BranchId == branchId &&
                product.ProductId != excludeProductId &&
                EF.Functions.ILike(product.ProductCode, trimmed));
    }

    public Task<bool> CategoryIsActiveAsync(int categoryId)
    {
        return _context.ProductCategories
            .AsNoTracking()
            .AnyAsync(category => category.CategoryId == categoryId && category.Status == "ACTIVE");
    }

    public async Task<ProductData> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var categoryName = await _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.CategoryId == product.CategoryId)
            .Select(category => category.CategoryName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return new ProductData
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            CategoryName = categoryName,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Price = product.Price,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<ProductData?> UpdateAsync(int id, int categoryId, string productName, decimal price, string? description, string? imageUrl)
    {
        var product = await _context.Products.FirstOrDefaultAsync(item => item.ProductId == id);
        if (product == null)
            return null;

        product.CategoryId = categoryId;
        product.ProductName = productName;
        product.Price = price;
        product.Description = description;
        product.ImageUrl = imageUrl;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var categoryName = await _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.CategoryId == categoryId)
            .Select(category => category.CategoryName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return new ProductData
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            CategoryName = categoryName,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Price = product.Price,
            Status = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(item => item.ProductId == id);
        if (product == null || product.Status != "ACTIVE")
            return false;

        product.Status = "INACTIVE";
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(item => item.ProductId == id);
        if (product == null || product.Status == "ACTIVE")
            return false;

        product.Status = "ACTIVE";
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
