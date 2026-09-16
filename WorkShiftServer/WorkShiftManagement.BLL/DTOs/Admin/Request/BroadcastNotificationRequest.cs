using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class BroadcastNotificationRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    [Required]
    public string Target { get; set; } = "Custom";

    [Required]
    [MinLength(1)]
    public List<string> TargetRoles { get; set; } = [];

    public int? RegionId { get; set; }

    public int? BranchId { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> Channels { get; set; } = [];
}
