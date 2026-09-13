namespace UI_Admin.Models;

public sealed class UserDetailViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNo { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }
    public int? PreviousBranchId { get; set; }
    public string? PreviousBranchCode { get; set; }
    public string? PreviousBranchName { get; set; }
    public int? EmployeeTypeId { get; set; }
    public string? EmployeeTypeName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsPending => Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
    public bool IsInactive => Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase);

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

    public string GenderDisplay => Gender.ToUpperInvariant() switch
    {
        "MALE" or "NAM" => "Male",
        "FEMALE" or "NU" or "NỮ" => "Female",
        _ => Gender
    };

    public int CurrentStep => IsActive ? 4 : (IsPending ? 2 : 1);
}
