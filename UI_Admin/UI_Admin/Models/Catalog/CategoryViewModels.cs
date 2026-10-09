using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Catalog;

public class CategoryListItemViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
}

public class CategoryListPagedViewModel
{
    public List<CategoryListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "Category Name is required.")]
    [StringLength(150, ErrorMessage = "Category Name cannot exceed 150 characters.")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class EditCategoryViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category Name is required.")]
    [StringLength(150, ErrorMessage = "Category Name cannot exceed 150 characters.")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public int ProductCount { get; set; }
}

public class CategoryProductItemViewModel
{
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class CategoryDetailViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
    public List<CategoryProductItemViewModel> Products { get; set; } = [];
}
