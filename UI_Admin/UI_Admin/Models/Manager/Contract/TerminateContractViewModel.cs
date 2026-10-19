using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Manager;

public class TerminateContractViewModel
{
    [Required]
    public int ContractId { get; set; }

    public string? ContractNumber { get; set; }
    public string? EmployeeFullName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do chấm dứt hợp đồng.")]
    [MaxLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
    [Display(Name = "Lý do chấm dứt")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Ngày có hiệu lực")]
    public DateOnly? TerminationEffectiveDate { get; set; }

    public string? ReturnUrl { get; set; }
}
