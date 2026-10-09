using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.UserAdmin;

public sealed class UserListQueryViewModel
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [StringLength(150)]
    public string? Search { get; set; }

    [RegularExpression("^(PENDING|ACTIVE|INACTIVE)$")]
    public string? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int? RoleId { get; set; }

    [Range(1, int.MaxValue)]
    public int? BranchId { get; set; }
}

public sealed class UserListViewModel
{
    public UserListQueryViewModel Query { get; set; } = new();
    public IReadOnlyList<UserListItemViewModel> Items { get; set; } = [];
    public IReadOnlyList<UserFilterOptionViewModel> Roles { get; set; } = [];
    public IReadOnlyList<UserFilterOptionViewModel> Branches { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int ActiveUsers { get; set; }
    public int ElevatedUsers { get; set; }
    public int PendingUsers { get; set; }
    public int ActiveBranches { get; set; }
    public int TotalBranches { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class UserListItemViewModel
{
    public Guid UserId { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? PreviousBranchId { get; set; }
    public string? PreviousBranchName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

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
}

public sealed class UserFilterOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
