using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.EmployeeTypes;

public class EmployeeTypeListItemViewModel
{
    public int EmployeeTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class EmployeeTypeListPagedViewModel
{
    public List<EmployeeTypeListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class EmployeeTypeFormViewModel
{
    [Display(Name = "Employee Type Name")]
    [Required(ErrorMessage = "Employee type name is required.")]
    [StringLength(100, ErrorMessage = "Employee type name cannot exceed 100 characters.")]
    public string TypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class EditEmployeeTypeViewModel : EmployeeTypeFormViewModel
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string Status { get; set; } = "ACTIVE";

    public int EmployeeTypeId { get; set; }
}

public class EmployeeTypeDetailViewModel : EmployeeTypeListItemViewModel
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
