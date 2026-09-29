using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateEmployeeTypeRequest
{
    [Required(ErrorMessage = "Employee type name is required.")]
    [StringLength(100, ErrorMessage = "Employee type name cannot exceed 100 characters.")]
    public string TypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

