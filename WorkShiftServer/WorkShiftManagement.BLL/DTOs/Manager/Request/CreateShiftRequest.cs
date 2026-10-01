using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class CreateShiftRequest
{
    [Range(1, int.MaxValue)]
    public int ShiftTemplateId { get; set; }
    public DateOnly WorkDate { get; set; }
    [StringLength(500)]
    public string? Note { get; set; }
    [Required, MinLength(1)]
    public List<ShiftEmployeeRequirementRequest> EmployeeRequirements { get; set; } = [];
}

public sealed class ShiftEmployeeRequirementRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeTypeId { get; set; }
    [Range(1, int.MaxValue)]
    public int RequiredCount { get; set; }
}
