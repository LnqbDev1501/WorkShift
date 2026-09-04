using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request
{
    public class RejectPayrollRequest
    {
        [Required(ErrorMessage = "Lý do yêu cầu chỉnh sửa là bắt buộc.")]
        [StringLength(
            1000,
            ErrorMessage = "Lý do không được vượt quá 1000 ký tự."
        )]
        public string Reason { get; set; } = string.Empty;
    }
}