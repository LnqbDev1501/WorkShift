using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class CreateAdminUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name must not exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    [RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identity number is required.")]
    [StringLength(30, ErrorMessage = "Identity number must not exceed 30 characters.")]
    [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "Identity number must contain either 9 or 12 digits.")]
    public string IdentityNo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "RoleId must be greater than 0.")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "BranchId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
    public string? Address { get; set; }

    [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters.")]
    public string? AvatarUrl { get; set; }
}
