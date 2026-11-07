using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Manager;

public class SendTerminationWarningViewModel
{
    [Required]
    public int ContractId { get; set; }

    public string? ContractNumber { get; set; }
    public string? EmployeeFullName { get; set; }

    public int WarningNo { get; set; }

    [Required(ErrorMessage = "Warning notification title is required.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Notification title must be between 3 and 150 characters.")]
    [Display(Name = "Notification Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warning notification content is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Notification content must be between 5 and 1000 characters.")]
    [Display(Name = "Notification Content")]
    public string Content { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
