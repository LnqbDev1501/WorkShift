using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftManagement.BLL.Services.Auth;

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ICloudinaryService _cloudinaryService;

    public ProfileService(AppDbContext context, IAuthService authService, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _authService = authService;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ProfileResponse?> GetMyProfileAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return null;

        var entity = await _context.Users.AsNoTracking()
            .Where(u => u.UserId == userId.Value && u.Status == "ACTIVE" && u.DeletedAt == null)
            .Select(u => new ProfileResponse
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
                RoleName = u.Role.RoleName,
                BranchId = u.BranchId,
                BranchCode = u.Branch == null ? null : u.Branch.BranchCode,
                BranchName = u.Branch == null ? null : u.Branch.BranchName,
                EmployeeTypeId = u.EmployeeTypeId,
                EmployeeTypeName = u.EmployeeType == null ? null : u.EmployeeType.TypeName,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .SingleOrDefaultAsync();

        return entity;
    }

    public async Task<ProfileResponse> UpdateMyProfileAsync(ClaimsPrincipal user, UpdateMyProfileRequest request)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length is 0 or > 150)
            throw new ArgumentException("Full name must contain between 1 and 150 characters.");

        var phone = request.Phone?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(phone, @"^0[35789]\d{8}$"))
            throw new ArgumentException("Phone number must contain 10 digits and use a valid Vietnamese mobile prefix.");

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

        var phoneOwner = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone && u.UserId != userId && u.DeletedAt == null);
        if (phoneOwner != null)
            ThrowIdentityConflict(phoneOwner.Status, "Phone number");

        entity.FullName = fullName;
        entity.Phone = phone;
        entity.Gender = gender;
        entity.DateOfBirth = request.DateOfBirth.Value;
        entity.Address = address;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetMyProfileAsync(user)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
    }

    public async Task<string> UpdateMyAvatarAsync(ClaimsPrincipal user, IFormFile? avatarFile)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
        if (avatarFile == null || avatarFile.Length == 0)
            throw new ArgumentException("Please select an image file.");

        var entity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        entity.AvatarUrl = await _cloudinaryService.UploadAvatarAsync(userId, avatarFile);
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return entity.AvatarUrl;
    }

    public async Task<PendingEmailChange> RequestEmailChangeAsync(ClaimsPrincipal user, string newEmail)
    {
        var userId = GetUserId(user) ?? throw new UnauthorizedAccessException("Unable to identify the current user.");

        var entity = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        var normalized = NormalizeEmail(newEmail);
        if (string.Equals(entity.Email, normalized, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The new email address is the same as the current one.");

        await EnsureEmailAvailableAsync(normalized, userId);

        return new PendingEmailChange { UserId = userId, FullName = entity.FullName, NewEmail = normalized };
    }

    public async Task<ProfileResponse> ConfirmEmailChangeAsync(Guid userId, string newEmail)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Invalid verification link.");
        var normalized = NormalizeEmail(newEmail);

        var entity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
        RequireActive(entity);

        if (string.Equals(entity.Email, normalized, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This email address is already in use.");

        await EnsureEmailAvailableAsync(normalized, userId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            entity.Email = normalized;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _authService.UpdateEmailAsync(userId, normalized);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())]));
        return await GetMyProfileAsync(principal)
            ?? throw new KeyNotFoundException("No matching user profile was found.");
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    private static void RequireActive(DAL.Entities.User entity)
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

    private async Task EnsureEmailAvailableAsync(string normalizedEmail, Guid excludeUserId)
    {
        var owner = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.UserId != excludeUserId && u.DeletedAt == null);
        if (owner != null)
            ThrowIdentityConflict(owner.Status, "Email address");
    }

    private static void ThrowIdentityConflict(string status, string fieldName)
    {
        if (status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{fieldName} belongs to an inactive account. Restore the existing account instead.");
        if (status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{fieldName} belongs to an account pending activation.");
        throw new InvalidOperationException($"{fieldName} is already in use.");
    }
}
