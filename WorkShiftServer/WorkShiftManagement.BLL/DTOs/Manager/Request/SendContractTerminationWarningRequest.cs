using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class SendContractTerminationWarningRequest
{
    [Required(ErrorMessage = "Warning notification title is required.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Notification title must be between 3 and 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warning notification content is required.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Notification content must be between 5 and 1000 characters.")]
    public string Content { get; set; } = string.Empty;
}
