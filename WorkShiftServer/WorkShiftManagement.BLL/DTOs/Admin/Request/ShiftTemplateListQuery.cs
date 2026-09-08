using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public sealed class ShiftTemplateListQuery
{
    [Range(1, 20, ErrorMessage = "Page must be between 1 and 20.")]
    public int Page { get; set; } = 1;

    public string? Search { get; set; }
}
