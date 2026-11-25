using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Admin.Request;

public class UpdateAdminUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name must not exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    [RegularExpression(@"^(03[2-9]|05[2689]|07[06-9]|08[1-689]|09[0-46-9])\d{7}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (032-039, 052, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090-094, 096-099).")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identity number is required.")]
    [StringLength(30, ErrorMessage = "Identity number must not exceed 30 characters.")]
    [RegularExpression(@"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$", ErrorMessage = "Identity number is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.")]
    public string IdentityNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Role ID is required.")]
    public int RoleId { get; set; }

    public Microsoft.AspNetCore.Http.IFormFile? AvatarFile { get; set; }
}
