using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.Manager;

public class ManagerProductListItemViewModel
{
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class ManagerProductListViewModel
{
    public List<ManagerProductListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string StatusFilter { get; set; } = "all";
    public List<CategoryOptionViewModel> Categories { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagerProductDetailViewModel
{
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class ManagerCreateProductViewModel
{
    [Required(ErrorMessage = "Please select a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Product Code is required.")]
    [StringLength(50, ErrorMessage = "Product Code cannot exceed 50 characters.")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product Name is required.")]
    [StringLength(150, ErrorMessage = "Product Name cannot exceed 150 characters.")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(1000, 999999999999999999.99, ErrorMessage = "Price must be at least 1,000 VND.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    public List<CategoryOptionViewModel> Categories { get; set; } = [];
}

public class ManagerEditProductViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product Name is required.")]
    [StringLength(150, ErrorMessage = "Product Name cannot exceed 150 characters.")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(1000, 999999999999999999.99, ErrorMessage = "Price must be at least 1,000 VND.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public List<CategoryOptionViewModel> Categories { get; set; } = [];
}