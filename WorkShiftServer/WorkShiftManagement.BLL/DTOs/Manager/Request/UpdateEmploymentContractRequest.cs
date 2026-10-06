using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class UpdateEmploymentContractRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    [Required, StringLength(100)]
    public string EmployeePosition { get; set; } = string.Empty;
    public string? ContractTerms { get; set; }
}
