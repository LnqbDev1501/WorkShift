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

    [Required(ErrorMessage = "Identity number is required.")]
    [RegularExpression(@"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$", ErrorMessage = "Identity number is invalid.")]
    public string IdentityNo { get; set; } = null!;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = null!;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }
}