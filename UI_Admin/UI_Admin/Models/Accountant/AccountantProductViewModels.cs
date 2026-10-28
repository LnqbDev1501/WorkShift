using System.ComponentModel.DataAnnotations;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Models.Accountant;

public class AccountantProductListItemViewModel
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

public class AccountantProductListViewModel
{
    public List<AccountantProductListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public int? CategoryId { get; set; }
    public string StatusFilter { get; set; } = "all";
    public List<CategoryOptionViewModel> Categories { get; set; } = [];
    public List<BranchOptionItemViewModel> Branches { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccountantProductDetailViewModel
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
