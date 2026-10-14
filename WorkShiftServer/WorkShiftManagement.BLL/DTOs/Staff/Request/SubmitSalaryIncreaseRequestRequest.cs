using System.ComponentModel.DataAnnotations;
namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public sealed class SubmitSalaryIncreaseRequestRequest
{
    public decimal ProposedHourlyRate { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
