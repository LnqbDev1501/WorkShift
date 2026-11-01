using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request
{
    public class RejectPayrollRequest
    {
        [Required]
        [RegularExpression("^(CALCULATION|RATE|REWARD|DEDUCTION|OTHER)$")]
        public string RejectionCategory { get; set; } = string.Empty;
        [Required(ErrorMessage = "Rejection reason is required.")]
        [StringLength(
            1000,
            ErrorMessage = "Rejection reason cannot exceed 1000 characters."
        )]
        public string Reason { get; set; } = string.Empty;
    }
}
