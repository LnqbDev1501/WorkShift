using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class RestoreAdminUserRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
    public int? BranchId { get; set; }
}
