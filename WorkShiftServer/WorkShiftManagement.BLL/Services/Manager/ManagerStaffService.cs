using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Upload;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerStaffService : IManagerStaffService
{
    private readonly IManagerStaffRepo _staffRepo;
    private readonly IAuthService _authService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly AppDbContext _context;

    public ManagerStaffService(
        IManagerStaffRepo staffRepo,
        IAuthService authService,
        ICloudinaryService cloudinaryService,
        AppDbContext context)
    {
        _staffRepo = staffRepo;
        _authService = authService;
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    public async Task<StaffAccountPagedResponse> GetStaffListAsync(ManagerStaffListQuery query, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        var pageNumber = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            status = query.Status.Trim().ToUpperInvariant();
            if (status != "ACTIVE" && status != "INACTIVE")
            {
                throw new ArgumentException("Status filter only accepts 'ACTIVE' or 'INACTIVE'.");
            }
        }

        var (items, totalCount) = await _staffRepo.GetPagedStaffAsync(
            branchId,
            query.Search,
            query.Status,
            pageNumber,
            pageSize);

        return new StaffAccountPagedResponse
        {
            Items = items.Select(MapToListItem).ToList(),
            Page = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StaffAccountDetailResponse?> GetStaffDetailAsync(Guid staffId, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);
        var staff = await _staffRepo.GetStaffByIdAsync(staffId, branchId);

        if (staff == null)
        {
            return null;
        }

        return MapToDetailResponse(staff);
    }

    public async Task<StaffAccountDetailResponse> CreateStaffAsync(CreateStaffAccountRequest request, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        await ValidateStaffFieldsAsync(request, branchId);

        if (await _staffRepo.ExistsByEmailAsync(normalizedEmail))
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        if (await _staffRepo.ExistsByPhoneAsync(request.Phone))
        {
            throw new InvalidOperationException("Phone number is already in use.");
        }

        if (await _staffRepo.ExistsByIdentityNoAsync(request.IdentityNo))
        {
            throw new InvalidOperationException("Identity number is already in use.");
        }

        var authUserId = await _authService.CreateUserAndSendInviteAsync(request.Email);
        var now = DateTime.UtcNow;

        var staff = new User
        {
            UserId = authUserId,
            BranchId = branchId,
            RoleId = await GetStaffRoleIdAsync(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            IdentityNo = request.IdentityNo.Trim(),
            Gender = request.Gender.Trim().ToUpperInvariant(),
            DateOfBirth = request.DateOfBirth.Value,
            Address = request.Address?.Trim(),
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _staffRepo.CreateStaffAsync(staff, managerId);

        var createdStaff = await _staffRepo.GetStaffByIdAsync(staff.UserId, branchId)
            ?? throw new InvalidOperationException("Failed to retrieve created staff.");

        return MapToDetailResponse(createdStaff);
    }

    public async Task<StaffAccountDetailResponse?> UpdateStaffAsync(Guid staffId, UpdateStaffAccountRequest request, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        var (fullName, phone, identityNo, gender, dateOfBirth, address) = ValidateUpdateFields(request);

        var staff = await _staffRepo.GetStaffByIdAsync(staffId, branchId);
        if (staff == null || staff.BranchId != branchId)
        {
            return null;
        }

        await ValidateUpdateFieldsAsync(phone, identityNo, branchId, staff.UserId);

        var updated = await _staffRepo.UpdateStaffAsync(
            staffId,
            branchId,
            fullName,
            phone,
            identityNo,
            gender,
            dateOfBirth,
            address);

        if (updated == null)
        {
            return null;
        }

        return MapToDetailResponse(updated);
    }

    public async Task<bool> DeactivateStaffAsync(Guid staffId, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        var staff = await _staffRepo.GetStaffByIdAsync(staffId, branchId);
        if (staff == null)
        {
            return false;
        }

        if (staff.Status != "ACTIVE")
        {
            throw new InvalidOperationException("Staff is already inactive.");
        }

        var hasPendingTasks = await CheckPendingTasksAsync(staff.UserId, branchId);
        if (hasPendingTasks)
        {
            throw new InvalidOperationException("Cannot deactivate staff with pending tasks (shifts, requests, etc.).");
        }

        return await _staffRepo.SoftDeleteStaffAsync(staffId, branchId);
    }

    public async Task<bool> RestoreStaffAsync(Guid staffId, Guid managerId, int? branchId)
    {
        var (_, managerBranchId) = await GetManagerAsync(managerId);

        var staff = await _staffRepo.GetStaffByIdAsync(staffId, managerBranchId);
        if (staff == null)
        {
            return false;
        }

        if (staff.Status != "INACTIVE" || staff.DeletedAt == null)
        {
            throw new InvalidOperationException("Staff is not in a restorable state.");
        }

        if (!await _staffRepo.ExistsByEmailAsync(staff.Email))
        {
        }
        else
        {
            var existing = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == staff.Email.ToLower() && u.UserId != staff.UserId && u.DeletedAt == null);
            if (existing != null)
            {
                throw new InvalidOperationException("Email is already in use by another active user.");
            }
        }

        return await _staffRepo.RestoreStaffAsync(staffId, managerBranchId, null);
    }

    private async Task<(string Role, int BranchId)> GetManagerAsync(Guid managerId)
    {
        var manager = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == managerId && u.Status == "ACTIVE" && u.DeletedAt == null)
            .Select(u => new { u.Role.RoleName, u.BranchId })
            .SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Manager not found or inactive.");

        if (manager.RoleName != "MANAGER")
        {
            throw new UnauthorizedAccessException("Only managers can manage staff accounts.");
        }

        if (!manager.BranchId.HasValue)
        {
            throw new UnauthorizedAccessException("Manager is not assigned to a branch.");
        }

        return (manager.RoleName, manager.BranchId.Value);
    }

    private async Task<int> GetStaffRoleIdAsync()
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(r => r.RoleName == "STAFF" && r.Status == "ACTIVE")
            .Select(r => r.RoleId)
            .SingleOrDefaultAsync();

        if (role == 0)
        {
            throw new InvalidOperationException("Staff role not found or inactive.");
        }

        return role;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Email is required.");
        }

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

    private static (string FullName, string Phone, string IdentityNo, string Gender, DateOnly? DateOfBirth, string? Address)
        ValidateUpdateFields(UpdateStaffAccountRequest request)
    {
        var fullName = request.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
        {
            throw new ArgumentException("Full name must be between 1 and 150 characters.");
        }

        var phone = request.Phone?.Trim();
        if (!Regex.IsMatch(phone ?? string.Empty, @"^0[35789]\d{8}$"))
        {
            throw new ArgumentException("Phone number must be a valid Vietnamese mobile number (10 digits, starting with 03/05/07/08/09).");
        }

        var identityNo = request.IdentityNo?.Trim();
        if (!Regex.IsMatch(identityNo ?? string.Empty, @"^\d{9}$|^\d{12}$"))
        {
            throw new ArgumentException("Identity number must contain either 9 or 12 digits.");
        }

        var gender = request.Gender?.Trim().ToUpperInvariant();
        if (gender is not ("MALE" or "FEMALE" or "OTHER"))
        {
            throw new ArgumentException("Gender must be MALE, FEMALE, or OTHER.");
        }

        if (!request.DateOfBirth.HasValue)
        {
            throw new ArgumentException("Date of birth is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth.Value < new DateOnly(1900, 1, 1))
        {
            throw new ArgumentException("Date of birth must be on or after January 1, 1900.");
        }
        if (request.DateOfBirth.Value > today.AddYears(-18))
        {
            throw new ArgumentException("Staff must be at least 18 years old.");
        }

        var address = request.Address?.Trim();
        if (!string.IsNullOrWhiteSpace(address) && address.Length > 500)
        {
            throw new ArgumentException("Address cannot exceed 500 characters.");
        }

        return (request.FullName.Trim(), phone, identityNo, gender, request.DateOfBirth.Value, request.Address?.Trim());
    }

    private async Task ValidateStaffFieldsAsync(CreateStaffAccountRequest request, int branchId)
    {
        var email = NormalizeEmail(request.Email);

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email && u.DeletedAt == null))
        {
            throw new ArgumentException("Email is already in use.");
        }

        if (await _context.Users.AnyAsync(u => u.Phone == request.Phone.Trim() && u.DeletedAt == null))
        {
            throw new InvalidOperationException("Phone number is already in use.");
        }

        if (await _context.Users.AnyAsync(u => u.IdentityNo == request.IdentityNo.Trim() && u.DeletedAt == null))
        {
            throw new InvalidOperationException("Identity number is already in use.");
        }

        if (!await _context.Roles.AnyAsync(r => r.RoleName == "STAFF" && r.Status == "ACTIVE"))
        {
            throw new ArgumentException("Staff role not found or inactive.");
        }

        var fullName = request.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
        {
            throw new ArgumentException("Full name must be between 1 and 150 characters.");
        }

        var phone = request.Phone?.Trim();
        if (!Regex.IsMatch(phone ?? string.Empty, @"^0[35789]\d{8}$"))
        {
            throw new ArgumentException("Phone number must be a valid Vietnamese mobile number (10 digits, starting with 03/05/07/08/09).");
        }

        if (!Regex.IsMatch(request.IdentityNo?.Trim() ?? string.Empty, @"^\d{9}$|^\d{12}$"))
        {
            throw new ArgumentException("Identity number must be 9 or 12 digits.");
        }

        var gender = request.Gender?.Trim().ToUpperInvariant();
        if (gender is not ("MALE" or "FEMALE" or "OTHER"))
        {
            throw new ArgumentException("Gender must be MALE, FEMALE, or OTHER.");
        }

        if (!request.DateOfBirth.HasValue)
        {
            throw new ArgumentException("Date of birth is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth.Value < new DateOnly(1900, 1, 1))
        {
            throw new ArgumentException("Date of birth must be on or after 1900-01-01.");
        }
        if (request.DateOfBirth.Value > today.AddYears(-18))
        {
            throw new ArgumentException("Staff must be at least 18 years old.");
        }

        var address = request.Address?.Trim();
        if (!string.IsNullOrWhiteSpace(address) && address.Length > 500)
        {
            throw new ArgumentException("Address cannot exceed 500 characters.");
        }
    }

    private async Task ValidateUpdateFieldsAsync(string phone, string identityNo, int branchId, Guid excludeUserId)
    {
        var trimmedPhone = phone.Trim();
        if (await _context.Users.AsNoTracking().AnyAsync(u => u.Phone == trimmedPhone && u.UserId != excludeUserId && u.DeletedAt == null))
        {
            throw new ArgumentException("Phone number is already in use.");
        }

        var trimmedIdentity = identityNo.Trim();
        if (await _context.Users.AsNoTracking().AnyAsync(u => u.IdentityNo == trimmedIdentity && u.UserId != excludeUserId && u.DeletedAt == null))
        {
            throw new ArgumentException("Identity number is already in use.");
        }
    }

    private async Task<bool> CheckPendingTasksAsync(Guid staffUserId, int branchId)
    {
        var hasShiftAssignments = await _context.ShiftAssignments
            .AnyAsync(sa => sa.UserId == staffUserId && sa.Status == "ASSIGNED");

        var hasPendingRequests = await _context.EmployeeRequests
            .AnyAsync(er => er.UserId == staffUserId && er.Status == "PENDING");

        var hasSwapRequests = await _context.ShiftSwapRequests
            .AnyAsync(sr => (sr.SourceAssignment.UserId == staffUserId || sr.TargetAssignment.UserId == staffUserId) && sr.Status == "PENDING");

        return hasShiftAssignments || hasPendingRequests || hasSwapRequests;
    }

    private static StaffAccountDetailResponse MapToDetailResponse(User staff)
    {
        return new StaffAccountDetailResponse
        {
            UserId = staff.UserId,
            FullName = staff.FullName,
            Email = staff.Email,
            Phone = staff.Phone,
            IdentityNo = staff.IdentityNo,
            Gender = staff.Gender,
            DateOfBirth = staff.DateOfBirth,
            Address = staff.Address,
            AvatarUrl = staff.AvatarUrl,
            Status = staff.Status,
            RoleName = staff.Role?.RoleName ?? "STAFF",
            BranchId = staff.BranchId ?? 0,
            BranchName = staff.Branch?.BranchName ?? string.Empty,
            BranchCode = staff.Branch?.BranchCode ?? string.Empty,
            CreatedAt = staff.CreatedAt,
            UpdatedAt = staff.UpdatedAt,
            DeletedAt = staff.DeletedAt,
            PreviousStatus = staff.PreviousStatus
        };
    }

    private static StaffAccountListItemResponse MapToListItem(User staff)
    {
        return new StaffAccountListItemResponse
        {
            UserId = staff.UserId,
            FullName = staff.FullName,
            Email = staff.Email,
            Phone = staff.Phone,
            Status = staff.Status,
            RoleName = staff.Role?.RoleName ?? "STAFF",
            BranchName = staff.Branch?.BranchName ?? string.Empty,
            CreatedAt = staff.CreatedAt
        };
    }
}