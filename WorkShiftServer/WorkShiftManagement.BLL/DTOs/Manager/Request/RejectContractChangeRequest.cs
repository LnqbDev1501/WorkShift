using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class RejectContractChangeRequest
{
    [Required(ErrorMessage = "Lý do từ chối yêu cầu là bắt buộc.")]
    [MaxLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
