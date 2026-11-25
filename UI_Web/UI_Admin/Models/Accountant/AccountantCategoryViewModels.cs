using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.Accountant;

public class AccountantCategoryListItemViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
}

public class AccountantCategoryListViewModel
{
    public List<AccountantCategoryListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccountantCategoryDetailViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int ProductCount { get; set; }
    public List<CategoryProductItemViewModel> Products { get; set; } = [];
}