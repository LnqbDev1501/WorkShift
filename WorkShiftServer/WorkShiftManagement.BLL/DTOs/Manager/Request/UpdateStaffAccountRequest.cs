using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public class UpdateStaffAccountRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = null!;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string Address { get; set; } = null!;

    /// <summary>
    /// Preserved for backward compatibility, not editable by manager.
    /// </summary>
    public string? IdentityNo { get; set; }
}