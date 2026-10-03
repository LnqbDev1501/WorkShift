using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class UpdateStaffAccountRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (03x, 05x, 07x, 08x, 09x).")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Identity number is required.")]
    [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "Identity number must contain either 9 or 12 numeric digits.")]
    public string IdentityNo { get; set; } = null!;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = null!;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }
}