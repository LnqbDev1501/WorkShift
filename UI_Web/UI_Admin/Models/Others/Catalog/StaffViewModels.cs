using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace UI_Admin.Models.Catalog;

public class StaffAccountListItemViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasPassword { get; set; } = true;
    public bool HasContract { get; set; }
}

public class StaffAccountPagedViewModel
{
    public List<StaffAccountListItemViewModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems
    {
        get => TotalCount;
        set => TotalCount = value;
    }

    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StaffAccountDetailViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNo { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? EmployeeTypeName { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? PreviousStatus { get; set; }
    public bool HasPassword { get; set; }
}

public class CreateStaffAccountViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$", ErrorMessage = "Phone number must be a valid 10-digit Vietnamese mobile number (032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identity number is required.")]
    [RegularExpression(@"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$", ErrorMessage = "Identity number is invalid.")]
    public string IdentityNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string Address { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; set; }

    public List<CategoryOptionViewModel> Categories { get; set; } = [];

    [Required(ErrorMessage = "Employee type is required.")]
    public int? EmployeeTypeId { get; set; }

    public List<EmployeeTypeOptionViewModel> EmployeeTypes { get; set; } = [];
}

public class EmployeeTypeOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class EditStaffAccountViewModel
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$", ErrorMessage = "Phone number must be a valid 10-digit Vietnamese mobile number (032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).")]
        public string Phone { get; set; } = string.Empty;

        public string IdentityNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression(@"^(MALE|FEMALE|OTHER)$", ErrorMessage = "Gender must be MALE, FEMALE, or OTHER.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
    }