using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UI_Admin.Models.Manager;

public class EditContractViewModel : IValidatableObject
{
    [BindNever, ValidateNever]
    public int ContractId { get; set; }

    [BindNever, ValidateNever]
    public string ContractNumber { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string EmployeeFullName { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string BranchName { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public string RegionName { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public decimal CurrentHourlyRate { get; set; }

    [BindNever, ValidateNever]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a start date.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Required(ErrorMessage = "Please enter a position/title.")]
    [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
    public string EmployeePosition { get; set; } = string.Empty;

    public string? ContractTerms { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<string> PositionOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default(DateOnly))
            yield return new("Invalid start date.", [nameof(StartDate)]);
        if (EndDate.HasValue && StartDate.HasValue && EndDate <= StartDate)
            yield return new("End date must be after start date.", [nameof(EndDate)]);
        if (string.IsNullOrWhiteSpace(EmployeePosition))
            yield return new("Please enter a position/title.", [nameof(EmployeePosition)]);
    }
}
