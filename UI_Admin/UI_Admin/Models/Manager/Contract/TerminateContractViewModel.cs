using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Manager;

public class TerminateContractViewModel
{
    [Required]
    public int ContractId { get; set; }

    public string? ContractNumber { get; set; }
    public string? EmployeeFullName { get; set; }

    [Required(ErrorMessage = "Please provide a reason for terminating the contract.")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    [Display(Name = "Termination Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Effective Date")]
    public DateOnly? TerminationEffectiveDate { get; set; }

    public string? ReturnUrl { get; set; }
}
