using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class RecordPaymentRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50, ErrorMessage = "Payment method cannot exceed 50 characters.")]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Reference number cannot exceed 100 characters.")]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
    public string? Note { get; set; }
}