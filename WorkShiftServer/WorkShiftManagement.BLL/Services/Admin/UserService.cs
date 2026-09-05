using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class UserService : IUserService
{
    private const string ManagerRole = "MANAGER";
    private const string AccountantRole = "ACCOUNTANT";
    private const string PendingStatus = "PENDING";
    private const string ActiveStatus = "ACTIVE";
    private const string InactiveStatus = "INACTIVE";
    private const string DefaultAvatarUrl = "https://ui-avatars.com/api/?background=random&name=User";
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private readonly IUserRepo _userRepo;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepo userRepo, IAuthService authService, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _authService = authService;
        _logger = logger;
    }

    public async Task<CreateAdminUserResponse> CreateAdminUserAsync(CreateAdminUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        NormalizeAndValidateRequest(request);

        var role = await _userRepo.GetActiveRoleByIdAsync(request.RoleId)
            ?? throw new ArgumentException("Role does not exist or is inactive.");

        var roleName = role.RoleName.Trim().ToUpperInvariant();
        var isManager = roleName == ManagerRole;
        var isAccountant = roleName == AccountantRole;
        if (!isManager && !isAccountant)
            throw new ArgumentException("Admin can only create Manager or Accountant accounts.");

        await ValidateBranchRuleAsync(request.BranchId!.Value, request.RoleId, isManager);
        await ValidateIdentityIsAvailableAsync(request.Email, request.Phone, request.IdentityNo);

        var authUserId = await _authService.CreateUserAndSendInviteAsync(request.Email);
        try
        {
            var now = DateTime.UtcNow;
            var user = new User
            {
                UserId = authUserId,
                RoleId = request.RoleId,
                BranchId = request.BranchId.Value,
                EmployeeTypeId = null,
                FullName = request.FullName,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth!.Value,
                AvatarUrl = DefaultAvatarUrl,
                Address = request.Address,
                IdentityNo = request.IdentityNo,
                Email = request.Email,
                Phone = request.Phone,
                Status = PendingStatus,
                PreviousStatus = null,
                DeletedAt = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _userRepo.Create(user);
            await _userRepo.SaveChangesAsync();

            return new CreateAdminUserResponse
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                BranchId = user.BranchId.Value,
                FullName = user.FullName,
                IdentityNo = user.IdentityNo,
                Email = user.Email,
                Phone = user.Phone,
                Status = user.Status,
                CreatedAt = ConvertToVietnamTime(user.CreatedAt),
                UpdatedAt = ConvertToVietnamTime(user.UpdatedAt)
            };
        }
        catch
        {
            try
            {
                await _authService.DeleteUserAsync(authUserId);
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(rollbackException,
                    "Unable to roll back Supabase Auth user {UserId} after database failure", authUserId);
            }
            throw;
        }
    }

    public async Task ActivateUserAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        if (user.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return;

        if (!user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The current account status does not allow activation.");

        user.Status = ActiveStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.SaveChangesAsync();
    }

    public async Task ResendSetupLinkAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        if (user.DeletedAt is not null)
            throw new InvalidOperationException("Deleted accounts cannot receive invitations.");

        if (!user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only accounts pending activation can receive another invitation.");

        await _authService.ResendSetupLinkAsync(user.UserId, user.Email);
    }

    private static DateTimeOffset ConvertToVietnamTime(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utcDateTime), VietnamTimeZone);
    }

    private async Task ValidateBranchRuleAsync(int branchId, int roleId, bool isManager)
    {
        if (!await _userRepo.ActiveBranchExistsAsync(branchId))
            throw new ArgumentException("Branch does not exist or is inactive.");

        if (await _userRepo.BranchHasRoleAsync(branchId, roleId))
            throw new InvalidOperationException(
                isManager ? "This branch already has a Manager." : "This branch already has an Accountant.");
    }

    private async Task ValidateIdentityIsAvailableAsync(string email, string phone, string identityNo)
    {
        var emailOwner = await _userRepo.GetByEmailAsync(email);
        if (emailOwner is not null) ThrowIdentityConflict(emailOwner, "Email");

        var phoneOwner = await _userRepo.GetByPhoneAsync(phone);
        if (phoneOwner is not null) ThrowIdentityConflict(phoneOwner, "Phone number");

        var identityOwner = await _userRepo.GetByIdentityNoAsync(identityNo);
        if (identityOwner is not null) ThrowIdentityConflict(identityOwner, "Identity number");
    }

    private static void ThrowIdentityConflict(User existingUser, string fieldName)
    {
        if (existingUser.Status.Equals(InactiveStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{fieldName} belongs to an inactive account. Restore the existing account instead.");
        if (existingUser.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{fieldName} belongs to an account pending activation.");
        if (existingUser.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{fieldName} is already in use.");
        throw new InvalidOperationException($"{fieldName} already exists.");
    }

    private static void NormalizeAndValidateRequest(CreateAdminUserRequest request)
    {
        request.FullName = request.FullName.Trim();
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.Phone = request.Phone.Trim();
        request.IdentityNo = request.IdentityNo.Trim();
        request.Gender = request.Gender.Trim().ToUpperInvariant();
        request.Address = request.Address.Trim();

        if (request.FullName.Length is 0 or > 150)
            throw new ArgumentException("Full name must contain between 1 and 150 characters.");
        try { _ = new MailAddress(request.Email); }
        catch (FormatException) { throw new ArgumentException("Email format is invalid."); }
        if (!Regex.IsMatch(request.Phone, @"^0[35789]\d{8}$"))
            throw new ArgumentException("Phone number must contain 10 digits and use a valid Vietnamese mobile prefix.");
        if (!Regex.IsMatch(request.IdentityNo, @"^\d{9}$|^\d{12}$"))
            throw new ArgumentException("Identity number must contain either 9 or 12 digits.");
        if (request.Gender is not ("MALE" or "FEMALE" or "OTHER"))
            throw new ArgumentException("Gender must be MALE, FEMALE, or OTHER.");
        if (!request.BranchId.HasValue || request.BranchId.Value <= 0)
            throw new ArgumentException("Manager and Accountant accounts must be assigned to a branch.");
        if (!request.DateOfBirth.HasValue)
            throw new ArgumentException("Date of birth is required.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth.Value < new DateOnly(1900, 1, 1))
            throw new ArgumentException("Date of birth must be on or after January 1, 1900.");
        if (request.DateOfBirth.Value > today.AddYears(-18))
            throw new ArgumentException("User must be at least 18 years old.");
        if (request.Address.Length is 0 or > 500)
            throw new ArgumentException("Address must contain between 1 and 500 characters.");
    }
}
