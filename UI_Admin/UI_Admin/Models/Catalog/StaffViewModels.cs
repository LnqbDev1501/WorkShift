using System.ComponentModel.DataAnnotations;

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
}

public class StaffAccountPagedViewModel
{
    public List<StaffAccountListItemViewModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = ListPaginationViewModel.DefaultPageSize;
    public int TotalItems { get; set; }
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
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? PreviousStatus { get; set; }
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
    [RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Phone number must be a valid Vietnamese mobile number (10 digits, starting with 03/05/07/08/09).")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identity number is required.")]
    [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "Identity number must be 9 or 12 digits.")]
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
}

public class EditStaffAccountViewModel
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0[35789]\d{8}$", ErrorMessage = "Phone number must be a valid Vietnamese mobile number (10 digits, starting with 03/05/07/08/09).")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identity number is required.")]
        [RegularExpression(@"^\d{9}$|^\d{12}$", ErrorMessage = "Identity number must be 9 or 12 digits.")]
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

        public IFormFile? ImageFile { get; set; }

        public List<CategoryOptionViewModel> Categories { get; set; } = [];
    }