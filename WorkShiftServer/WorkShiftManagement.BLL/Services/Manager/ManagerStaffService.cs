using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkShiftManagement.BLL.Caching;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ManagerStaffService : IManagerStaffService
{
    private const string IdentityNoPattern = @"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$";
    private readonly IManagerStaffRepo _staffRepo;
    private readonly IAuthService _authService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IEmployeeTypeService _employeeTypeService;
    private readonly AppDbContext _context;
    private readonly string _defaultAvatarUrl;
    private readonly IMemoryCache? _memoryCache;

    public ManagerStaffService(
        IManagerStaffRepo staffRepo,
        IAuthService authService,
        ICloudinaryService cloudinaryService,
        IEmployeeTypeService employeeTypeService,
        AppDbContext context,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IMemoryCache? memoryCache = null)
    {
        _staffRepo = staffRepo;
        _authService = authService;
        _cloudinaryService = cloudinaryService;
        _employeeTypeService = employeeTypeService;
        _context = context;
        _defaultAvatarUrl = configuration["Cloudinary:DefaultAvatarUrl"]
            ?? throw new InvalidOperationException("Cloudinary:DefaultAvatarUrl is not configured.");
        _memoryCache = memoryCache;
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
            if (status != "ACTIVE" && status != "INACTIVE" && status != "PENDING")
            {
                throw new ArgumentException("Status filter only accepts 'ACTIVE', 'PENDING', or 'INACTIVE'.");
            }
        }

        var (items, totalCount) = await _staffRepo.GetPagedStaffAsync(
            branchId,
            query.Search,
            status,
            pageNumber,
            pageSize,
            query.HasContract);

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

        return await MapToDetailResponse(staff);
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

        Guid authUserId;
        try
        {
            authUserId = await _authService.CreateStaffUserAsync(request.Email);
        }
        catch (HttpRequestException exception) when (exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This email is already registered. Please use another email address or delete the existing authentication account.");
        }

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
            DateOfBirth = request.DateOfBirth!.Value,
            AvatarUrl = _defaultAvatarUrl,
            Address = request.Address?.Trim() ?? string.Empty,
            EmployeeTypeId = request.EmployeeTypeId,
            Status = "PENDING",
            HasPassword = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _staffRepo.CreateStaffAsync(staff, managerId);
        }
        catch
        {
            // Roll back the Supabase user so a failed DB insert does not orphan an auth account.
            try { await _authService.DeleteUserAsync(authUserId); } catch { /* best effort */ }
            throw;
        }

        var createdStaff = await _staffRepo.GetStaffByIdAsync(staff.UserId, branchId)
            ?? throw new InvalidOperationException("Failed to retrieve created staff.");

        return await MapToDetailResponse(createdStaff);
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

        if (staff.DeletedAt != null || (!staff.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) && !staff.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Deactivated staff cannot be edited. Restore the account first.");
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

        return await MapToDetailResponse(updated);
    }

    public async Task<bool> DeactivateStaffAsync(Guid staffId, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        var staff = await _staffRepo.GetStaffByIdAsync(staffId, branchId);
        if (staff == null)
        {
            return false;
        }

        if (staff.DeletedAt != null || (!staff.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) && !staff.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Staff is already inactive.");
        }

        var blockingReason = await GetDeactivationBlockingReasonAsync(staff.UserId, branchId);
        if (blockingReason != null)
        {
            throw new InvalidOperationException(blockingReason);
        }

        var deactivated = await _staffRepo.SoftDeleteStaffAsync(staffId, branchId);
        if (deactivated)
        {
            _memoryCache?.Remove(WorkShiftCacheKeys.UserRole(staffId));
        }

        return deactivated;
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

        var restored = await _staffRepo.RestoreStaffAsync(staffId, managerBranchId, null);
        if (restored)
        {
            _memoryCache?.Remove(WorkShiftCacheKeys.UserRole(staffId));
        }

        return restored;
    }

    public async Task<bool> ResendStaffSetupLinkAsync(Guid staffId, Guid managerId)
    {
        var (_, branchId) = await GetManagerAsync(managerId);

        var staff = await _staffRepo.GetStaffByIdAsync(staffId, branchId);
        if (staff == null || staff.BranchId != branchId)
        {
            return false;
        }

        // Check if user already has a password set (local DB)
        if (staff.HasPassword)
        {
            return false;
        }

        // User exists, belongs to manager's branch, and hasn't set password yet
        // Token generation and email sending will be handled by the API controller
        return true;
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
        if (!Regex.IsMatch(phone ?? string.Empty, @"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$"))
        {
            throw new ArgumentException("Phone number must be a valid Vietnamese mobile number (10 digits, valid prefix: 032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).");
        }

        var identityNo = request.IdentityNo?.Trim();
        if (!Regex.IsMatch(identityNo ?? string.Empty, IdentityNoPattern))
        {
            throw new ArgumentException("Identity number is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.");
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

        ValidateDateOfBirth(request.DateOfBirth.Value);

        var address = request.Address?.Trim() ?? string.Empty;
        if (address.Length is 0 or > 500)
        {
            throw new ArgumentException("Address must contain between 1 and 500 characters.");
        }

        return (fullName!, phone!, identityNo!, gender!, request.DateOfBirth, address);
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

        if (await _staffRepo.ExistsByIdentityNoAsync(request.IdentityNo))
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
        if (!Regex.IsMatch(phone ?? string.Empty, @"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$"))
        {
            throw new ArgumentException("Phone number must be a valid Vietnamese mobile number (10 digits, valid prefix: 032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).");
        }

        if (!Regex.IsMatch(request.IdentityNo?.Trim() ?? string.Empty, IdentityNoPattern))
        {
            throw new ArgumentException("Identity number is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.");
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

        ValidateDateOfBirth(request.DateOfBirth.Value);

        var address = request.Address?.Trim() ?? string.Empty;
        if (address.Length is 0 or > 500)
        {
            throw new ArgumentException("Address must contain between 1 and 500 characters.");
        }

        if (!request.EmployeeTypeId.HasValue)
        {
            throw new ArgumentException("Employee type is required.");
        }

        {
            var employeeType = await _employeeTypeService.GetByIdAsync(request.EmployeeTypeId.Value);
            if (employeeType == null || !employeeType.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Employee type was not found or is inactive.");
            }
        }
    }

    private static void ValidateDateOfBirth(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > today)
        {
            throw new ArgumentException("Date of birth cannot be in the future.");
        }
        if (dateOfBirth > today.AddYears(-18))
        {
            throw new ArgumentException("Staff must be at least 18 years old.");
        }
        if (dateOfBirth < today.AddYears(-99))
        {
            throw new ArgumentException("Staff must be at most 99 years old.");
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
        if (await _staffRepo.ExistsByIdentityNoAsync(trimmedIdentity, excludeUserId))
        {
            throw new ArgumentException("Identity number is already in use.");
        }
    }

    private async Task<string?> GetDeactivationBlockingReasonAsync(Guid staffUserId, int branchId)
    {
        var hasShiftAssignments = await _context.ShiftAssignments
            .AnyAsync(sa => sa.UserId == staffUserId && (sa.Status == "ASSIGNED" || sa.Status == "IN_PROGRESS"));
        if (hasShiftAssignments)
        {
            return "Cannot deactivate staff with assigned upcoming or ongoing shifts. Please reassign, complete, or cancel those shifts first.";
        }

        var hasActiveAttendance = await _context.Attendances
            .AnyAsync(a => a.Assignment.UserId == staffUserId && a.Status == "IN_PROGRESS");
        if (hasActiveAttendance)
        {
            return "Cannot deactivate staff currently clocked into an active shift. Please ensure the shift is completed and checked out first.";
        }

        var hasPendingRequests = await _context.EmployeeRequests
            .AnyAsync(er => er.UserId == staffUserId && er.Status == "PENDING");
        if (hasPendingRequests)
        {
            return "Cannot deactivate staff with pending requests (leave, overtime, etc.). Please review them first.";
        }

        var hasSwapRequests = await _context.ShiftSwapRequests
            .AnyAsync(sr => (sr.SourceAssignment.UserId == staffUserId || sr.TargetAssignment.UserId == staffUserId) && sr.Status == "PENDING");
        if (hasSwapRequests)
        {
            return "Cannot deactivate staff with pending shift swap requests. Please review them first.";
        }

        var hasPendingTimesheetAdjustments = await _context.TimesheetAdjustmentRequests
            .AnyAsync(tar => tar.UserId == staffUserId && tar.Status == "PENDING");
        if (hasPendingTimesheetAdjustments)
        {
            return "Cannot deactivate staff with pending timesheet adjustment requests. Please review all timesheet disputes first.";
        }

        var hasPendingPayrollCorrections = await _context.PayrollCorrectionRequests
            .AnyAsync(pcr => pcr.Payroll.UserId == staffUserId && (pcr.Status == "OPEN" || pcr.Status == "RESPONDED"));
        if (hasPendingPayrollCorrections)
        {
            return "Cannot deactivate staff with open payroll correction requests. Please resolve all payroll disputes first.";
        }

        var hasPendingPayrollFeedback = await _context.PayrollFeedbackRequests
            .AnyAsync(pfr => pfr.EmployeeId == staffUserId && pfr.ReviewStatus == "PENDING");
        if (hasPendingPayrollFeedback)
        {
            return "Cannot deactivate staff with pending payroll preview reviews. Please wait for the employee to review their payroll.";
        }

        var hasPendingAdjustments = await _context.EmployeeRewardsPenalties
            .AnyAsync(erp => erp.UserId == staffUserId && !erp.IsDeleted && erp.ApprovalStatus == "PENDING");
        if (hasPendingAdjustments)
        {
            return "Cannot deactivate staff with pending bonus/penalty adjustments. Please review all adjustments first.";
        }

        var hasPendingSalaryChanges = await _context.SalaryChangeRequests
            .AnyAsync(scr => scr.UserId == staffUserId && scr.Status == "PENDING");
        if (hasPendingSalaryChanges)
        {
            return "Cannot deactivate staff with pending salary change proposals. Please resolve the salary change request first.";
        }

        var hasPendingContractChanges = await _context.ContractChangeRequests
            .AnyAsync(ccr => ccr.Contract.UserId == staffUserId && ccr.Status == "SUBMITTED");
        if (hasPendingContractChanges)
        {
            return "Cannot deactivate staff with pending contract change requests. Please review the contract changes first.";
        }

        var hasUnsettledPayroll = await _context.Timesheets
            .AnyAsync(t => t.UserId == staffUserId
                        && t.PayrollPeriod.Status != "PAID"
                        && t.PayrollPeriod.Status != "CLOSED"
                        && t.PayrollPeriod.Status != "CANCELLED")
            || await _context.Payrolls
            .AnyAsync(p => p.UserId == staffUserId
                        && p.PayrollPeriod.Status != "PAID"
                        && p.PayrollPeriod.Status != "CLOSED"
                        && p.PayrollPeriod.Status != "CANCELLED");
        if (hasUnsettledPayroll)
        {
            return "Cannot deactivate staff with records in an unsettled payroll period. Please wait until the payroll period is settled and closed.";
        }

        return null;
    }

    private async Task<StaffAccountDetailResponse> MapToDetailResponse(User staff)
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
            EmployeeTypeId = staff.EmployeeTypeId,
            EmployeeTypeName = staff.EmployeeType?.TypeName,
            BranchId = staff.BranchId ?? 0,
            BranchName = staff.Branch?.BranchName ?? string.Empty,
            BranchCode = staff.Branch?.BranchCode ?? string.Empty,
            CreatedAt = staff.CreatedAt,
            UpdatedAt = staff.UpdatedAt,
            DeletedAt = staff.DeletedAt,
            PreviousStatus = staff.PreviousStatus,
            HasPassword = staff.HasPassword
        };
    }

    private static StaffAccountListItemResponse MapToListItem(User staff)
    {
        var hasContract = staff.EmploymentContracts?.Any(c =>
            c.Status == "ACTIVE" || c.Status == "SCHEDULED" ||
            c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL") ?? false;

        return new StaffAccountListItemResponse
        {
            UserId = staff.UserId,
            FullName = staff.FullName,
            Email = staff.Email,
            Phone = staff.Phone,
            Status = staff.Status,
            RoleName = staff.Role?.RoleName ?? "STAFF",
            BranchName = staff.Branch?.BranchName ?? string.Empty,
            CreatedAt = staff.CreatedAt,
            HasPassword = staff.HasPassword,
            HasContract = hasContract
        };
    }
}