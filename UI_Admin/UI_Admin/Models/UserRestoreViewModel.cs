using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class UserRestoreViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Initials { get; set; } = string.Empty;

    public int? PreviousBranchId { get; set; }
    public string? PreviousBranchName { get; set; }

    [Required(ErrorMessage = "Please select a target branch for this account.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid branch.")]
    [Display(Name = "Target Branch")]
    public int? BranchId { get; set; }

    public List<BranchOptionViewModel> AvailableBranches { get; set; } = new();
}
