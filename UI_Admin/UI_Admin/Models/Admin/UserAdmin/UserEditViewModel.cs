using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace UI_Admin.Models.Admin.UserAdmin;

public sealed class UserEditViewModel
{
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name must not exceed 150 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Official work email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    [Display(Name = "Official Work Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile phone number is required.")]
    [RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (03x, 05x, 07x, 08x, 09x).")]
    [Display(Name = "Mobile Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "National citizen ID is required.")]
    [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "National Citizen ID must contain either 9 or 12 numeric digits.")]
    [Display(Name = "National Citizen ID (CCCD/CMND)")]
    public string IdentityNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "System role is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid system role.")]
    [Display(Name = "System Role")]
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
    public bool IsAdmin => RoleName.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

    [Display(Name = "Target Branch")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = "MALE";

    [Required(ErrorMessage = "Date of birth is required.")]
    [Display(Name = "Date of Birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Residential address is required.")]
    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
    [Display(Name = "Residential Address")]
    public string Address { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
    public IFormFile? AvatarFile { get; set; }

    public string Status { get; set; } = "ACTIVE";
    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);

    public string Initials
    {
        get
        {
            var words = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? "U"
                : string.Concat(words.TakeLast(2).Select(word => char.ToUpperInvariant(word[0])));
        }
    }

    public IReadOnlyList<BranchOptionViewModel> AvailableBranches { get; set; } = [];
    public IReadOnlyList<UserFilterOptionViewModel> AvailableRoles { get; set; } = [];
}
