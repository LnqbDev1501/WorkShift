using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.UserAdmin;

public sealed class UserCreateViewModel
{
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
    [RegularExpression(@"^(03[2-9]|05[2689]|07[06-9]|08[1-689]|09[0-46-9])\d{7}$", ErrorMessage = "Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (032-039, 052, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090-094, 096-099).")]
    [Display(Name = "Mobile Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "National citizen ID is required.")]
    [RegularExpression(@"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$", ErrorMessage = "National Citizen ID is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.")]
    [Display(Name = "National Citizen ID (CCCD/CMND)")]
    public string IdentityNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "System role is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid system role.")]
    [Display(Name = "System Role")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Target branch is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid branch.")]
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

    public IReadOnlyList<BranchOptionViewModel> AvailableBranches { get; set; } = [];
    public IReadOnlyList<UserFilterOptionViewModel> AvailableRoles { get; set; } = [];
}

public sealed class BranchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public int ManagerCount { get; set; }
    public int AccountantCount { get; set; }
    public bool HasManager => ManagerCount >= 1;
    public bool HasAccountant => AccountantCount >= 1;

    public string DisplayText =>
        string.IsNullOrWhiteSpace(BranchCode)
            ? $"{Name} — [Manager: {ManagerCount}/1, Accountant: {AccountantCount}/1]"
            : $"{Name} ({BranchCode}) — [Manager: {ManagerCount}/1, Accountant: {AccountantCount}/1]";
}


