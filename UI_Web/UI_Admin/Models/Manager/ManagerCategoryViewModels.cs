using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.Manager;

public class ManagerCategoryListItemViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
}

public class ManagerCategoryListViewModel
{
    public List<ManagerCategoryListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagerCategoryDetailViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
    public List<CategoryProductItemViewModel> Products { get; set; } = [];
}

public class ManagerCreateCategoryViewModel
{
    [Required(ErrorMessage = "Category Name is required.")]
    [StringLength(150, ErrorMessage = "Category Name cannot exceed 150 characters.")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class ManagerEditCategoryViewModel
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