using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateEmployeeTypeRequest
{
    private string _typeName = string.Empty;
    private string? _description;

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

