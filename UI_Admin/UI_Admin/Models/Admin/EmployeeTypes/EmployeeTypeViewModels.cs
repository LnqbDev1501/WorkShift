using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.EmployeeTypes;

public class EmployeeTypeListItemViewModel
{
    public int EmployeeTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class EmployeeTypeListPagedViewModel
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsLoaded { get; set; }
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
    private string _typeName = string.Empty;
    private string? _description;

    [Display(Name = "Employee Type Name")]
    [Required(ErrorMessage = "Employee type name is required.")]
    [StringLength(100, ErrorMessage = "Employee type name cannot exceed 100 characters.")]
    public string TypeName
    {
        get => _typeName;
        set => _typeName = value?.Trim() ?? string.Empty;
    }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description
    {
        get => _description;
        set => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
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
