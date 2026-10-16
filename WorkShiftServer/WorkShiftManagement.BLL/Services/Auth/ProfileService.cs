using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Auth;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepo _userRepo;
    private readonly IAuthService _authService;
    private readonly ICloudinaryService _cloudinaryService;

    public ProfileService(IUserRepo userRepo, IAuthService authService, ICloudinaryService cloudinaryService)
    {
        _userRepo = userRepo;
        _authService = authService;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ProfileResponse?> GetMyProfileAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return null;

        var entity = await _userRepo.GetActiveProfileByIdAsync(userId.Value);

        return entity == null ? null : MapToProfileResponse(entity);
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(ClaimsPrincipal user, UpdateMyProfileRequest request)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _userRepo.GetByIdWithTrackingAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length is 0 or > 150)
            throw new ArgumentException("Full name must contain between 1 and 150 characters.");

        var phone = request.Phone?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(phone, @"^0[35789]\d{8}$"))
            throw new ArgumentException("Phone number must contain 10 digits and use a valid Vietnamese mobile prefix.");

        if (await _userRepo.ExistsByPhoneAsync(phone, userId))
            ThrowIdentityConflict("Phone number");

        var gender = request.Gender?.Trim().ToUpperInvariant() ?? string.Empty;
        if (gender is not ("MALE" or "FEMALE" or "OTHER"))
            throw new ArgumentException("Gender must be MALE, FEMALE, or OTHER.");

        if (!request.DateOfBirth.HasValue)
            throw new ArgumentException("Date of birth is required.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth.Value < new DateOnly(1900, 1, 1))
            throw new ArgumentException("Date of birth must be on or after January 1, 1900.");
        if (request.DateOfBirth.Value > today.AddYears(-18))
            throw new ArgumentException("User must be at least 18 years old.");

        var address = request.Address?.Trim() ?? string.Empty;
        if (address.Length is 0 or > 500)
            throw new ArgumentException("Address must contain between 1 and 500 characters.");

        entity.FullName = fullName;
        entity.Phone = phone;
        entity.Gender = gender;
        entity.DateOfBirth = request.DateOfBirth.Value;
        entity.Address = address;
        entity.UpdatedAt = DateTime.UtcNow;
        await _userRepo.SaveChangesAsync();

        var updated = await _userRepo.GetActiveProfileByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        return MapToProfileResponse(updated);
    }

    public async Task<string> UpdateMyAvatarAsync(ClaimsPrincipal user, IFormFile? avatarFile)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
        if (avatarFile == null || avatarFile.Length == 0)
            throw new ArgumentException("Please select an image file.");

        var entity = await _userRepo.GetByIdWithTrackingAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        entity.AvatarUrl = await _cloudinaryService.UploadAvatarAsync(userId, avatarFile);
        entity.UpdatedAt = DateTime.UtcNow;
        await _userRepo.SaveChangesAsync();

        return entity.AvatarUrl;
    }

    public async Task<PendingEmailChange> RequestEmailChangeAsync(ClaimsPrincipal user, string newEmail)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");

        var entity = await _userRepo.GetByIdWithTrackingAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        var normalized = NormalizeEmail(newEmail);
        if (string.Equals(entity.Email, normalized, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The new email address is the same as the current one.");

        if (await _userRepo.ExistsByEmailAsync(normalized, userId))
            throw new InvalidOperationException("Email address is already in use.");

        return new PendingEmailChange { UserId = userId, FullName = entity.FullName, NewEmail = normalized };
    }

    public async Task<ProfileResponse> ConfirmEmailChangeAsync(Guid userId, string newEmail)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Invalid verification link.");
        var normalized = NormalizeEmail(newEmail);

        var entity = await _userRepo.GetByIdWithTrackingAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        if (string.Equals(entity.Email, normalized, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This email address is already in use.");

        if (await _userRepo.ExistsByEmailAsync(normalized, userId))
            throw new InvalidOperationException("Email address is already in use.");

        entity.Email = normalized;
        entity.UpdatedAt = DateTime.UtcNow;
        await _userRepo.SaveChangesAsync();

        await _authService.UpdateEmailAsync(userId, normalized);

        var updated = await _userRepo.GetActiveProfileByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        return MapToProfileResponse(updated);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    private static void RequireActive(User entity)
    {
        if (entity.DeletedAt.HasValue)
            throw new InvalidOperationException("Deleted accounts cannot be updated.");
        if (!entity.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only active accounts can be updated.");
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Email address is required.");
        try
        {
            _ = new MailAddress(normalized);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email format is invalid.");
        }
        return normalized;
    }

    private static void ThrowIdentityConflict(string fieldName)
    {
        throw new InvalidOperationException($"{fieldName} is already in use.");
    }

    private static ProfileResponse MapToProfileResponse(User u)
    {
        return new ProfileResponse
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            IdentityNo = u.IdentityNo,
            Gender = u.Gender,
            DateOfBirth = u.DateOfBirth,
            AvatarUrl = u.AvatarUrl,
            Address = u.Address,
            RoleId = u.RoleId,
            RoleName = u.Role?.RoleName,
            BranchId = u.BranchId,
            BranchCode = u.Branch?.BranchCode,
            BranchName = u.Branch?.BranchName,
            EmployeeTypeId = u.EmployeeTypeId,
            EmployeeTypeName = u.EmployeeType?.TypeName,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}
