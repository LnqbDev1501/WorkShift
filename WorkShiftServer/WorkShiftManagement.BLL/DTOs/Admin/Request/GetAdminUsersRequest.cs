using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class GetAdminUsersRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    public string? Search { get; set; }

    [RegularExpression("^(PENDING|ACTIVE|INACTIVE)$",
        ErrorMessage = "Status must be PENDING, ACTIVE, or INACTIVE.")]
    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RoleId must be greater than 0.")]
    public int? RoleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
    public int? BranchId { get; set; }
}
