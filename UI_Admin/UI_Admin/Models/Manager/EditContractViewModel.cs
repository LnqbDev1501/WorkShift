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

    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập chức danh.")]
    [StringLength(100, ErrorMessage = "Chức danh tối đa 100 ký tự.")]
    public string EmployeePosition { get; set; } = string.Empty;

    public string? ContractTerms { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate == default(DateOnly))
            yield return new("Ngày bắt đầu không hợp lệ.", [nameof(StartDate)]);
        if (EndDate.HasValue && StartDate.HasValue && EndDate <= StartDate)
            yield return new("Ngày kết thúc phải sau ngày bắt đầu.", [nameof(EndDate)]);
        if (string.IsNullOrWhiteSpace(EmployeePosition))
            yield return new("Vui lòng nhập chức danh.", [nameof(EmployeePosition)]);
    }
}
