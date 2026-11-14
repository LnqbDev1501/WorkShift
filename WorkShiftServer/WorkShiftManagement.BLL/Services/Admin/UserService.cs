using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.Caching;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Common;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Security;
using WorkShiftManagement.BLL.Interfaces.Upload;
using NotificationEntity = WorkShiftManagement.DAL.Entities.Notification;

namespace WorkShiftManagement.BLL.Services.Admin;

public class UserService : IUserService
{
    private const int AdminUserPageSize = 20;
    private const string ManagerRole = "MANAGER";
    private const string AccountantRole = "ACCOUNTANT";
    private const string PendingStatus = "PENDING";
    private const string ActiveStatus = "ACTIVE";
    private const string InactiveStatus = "INACTIVE";
    private const string IdentityNoPattern = @"^(001|002|004|006|008|010|011|012|014|015|017|019|020|022|024|025|026|027|030|031|033|034|035|036|037|038|040|042|044|045|046|048|049|051|052|054|056|058|060|062|064|066|067|068|070|072|074|075|077|079|080|082|083|084|086|087|089|091|092|093|094|095|096)\d{6}(\d{3})?$";
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private readonly IUserRepo _userRepo;
    private readonly IAuthService _authService;
    private readonly IAccountSetupEmailSender _accountSetupEmailSender;
    private readonly ILogger<UserService> _logger;
    private readonly string _defaultAvatarUrl;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IAdminRealtimePublisher _adminRealtimePublisher;
    private readonly INotificationRealtimePublisher _notificationRealtimePublisher;
    private readonly IAuditLogHelper? _auditLogHelper;
    private readonly IMemoryCache? _memoryCache;

    public UserService(
        IUserRepo userRepo,
        IAuthService authService,
        IAccountSetupEmailSender accountSetupEmailSender,
        ILogger<UserService> logger,
        IConfiguration configuration,
        ICloudinaryService cloudinaryService,
        IAdminRealtimePublisher adminRealtimePublisher,
        INotificationRealtimePublisher notificationRealtimePublisher,
        IAuditLogHelper? auditLogHelper = null,
        IMemoryCache? memoryCache = null)
    {
        _userRepo = userRepo;
        _authService = authService;
        _accountSetupEmailSender = accountSetupEmailSender;
        _logger = logger;
        _defaultAvatarUrl = configuration["Cloudinary:DefaultAvatarUrl"]
            ?? throw new InvalidOperationException(
                "Cloudinary:DefaultAvatarUrl is not configured.");
        _cloudinaryService = cloudinaryService;
        _adminRealtimePublisher = adminRealtimePublisher;
        _notificationRealtimePublisher = notificationRealtimePublisher;
        _auditLogHelper = auditLogHelper;
        _memoryCache = memoryCache;
    }

    public async Task<AdminUserListResponse> GetAdminUsersAsync(GetAdminUsersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber <= 0)
            throw new ArgumentException("PageNumber must be greater than 0.");
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToUpperInvariant();
        if (status is not null and not (PendingStatus or ActiveStatus or InactiveStatus))
            throw new ArgumentException("Status must be PENDING, ACTIVE, or INACTIVE.");
        if (request.RoleId <= 0)
            throw new ArgumentException("RoleId must be greater than 0.");
        if (request.BranchId <= 0)
            throw new ArgumentException("BranchId must be greater than 0.");

        var (users, totalItems) = await _userRepo.GetPagedAsync(
            request.PageNumber,
            AdminUserPageSize,
            search,
            status,
            request.RoleId,
            request.BranchId);

        return new AdminUserListResponse
        {
            Items = users.Select(user => new AdminUserListItemResponse
            {
                UserId = user.UserId,
                AvatarUrl = user.AvatarUrl,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                RoleName = user.Role.RoleName,
                BranchId = user.BranchId,
                BranchName = user.Branch?.BranchName,
                PreviousBranchId = user.PreviousBranchId,
                PreviousBranchName = user.PreviousBranch?.BranchName,
                Status = user.Status,
                DeletedAt = user.DeletedAt.HasValue
                    ? ConvertToVietnamTime(user.DeletedAt.Value)
                    : null,
                CreatedAt = ConvertToVietnamTime(user.CreatedAt),
                UpdatedAt = ConvertToVietnamTime(user.UpdatedAt)
            }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = AdminUserPageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)AdminUserPageSize)
        };
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAssignableRolesAsync()
    {
        var roles = await _userRepo.GetAssignableRolesAsync();
        return roles.Select(role => new RoleResponse
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description
        }).ToList();
    }

    public async Task<AdminUserDetailResponse> GetAdminUserDetailAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var user = await _userRepo.GetDetailByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        return new AdminUserDetailResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            IdentityNo = user.IdentityNo,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            AvatarUrl = user.AvatarUrl,
            Address = user.Address,
            RoleId = user.RoleId,
            RoleName = user.Role.RoleName,
            BranchId = user.BranchId,
            BranchCode = user.Branch?.BranchCode,
            BranchName = user.Branch?.BranchName,
            PreviousBranchId = user.PreviousBranchId,
            PreviousBranchCode = user.PreviousBranch?.BranchCode,
            PreviousBranchName = user.PreviousBranch?.BranchName,
            EmployeeTypeId = user.EmployeeTypeId,
            EmployeeTypeName = user.EmployeeType?.TypeName,
            Status = user.Status,
            PreviousStatus = user.PreviousStatus,
            DeletedAt = user.DeletedAt.HasValue
                ? ConvertToVietnamTime(user.DeletedAt.Value)
                : null,
            CreatedAt = ConvertToVietnamTime(user.CreatedAt),
            UpdatedAt = ConvertToVietnamTime(user.UpdatedAt)
        };
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
        User createdUser;
        CreateAdminUserResponse response;
        try
        {
            var now = DateTime.UtcNow;
            createdUser = new User
            {
                UserId = authUserId,
                RoleId = request.RoleId,
                BranchId = request.BranchId.Value,
                EmployeeTypeId = null,
                FullName = request.FullName,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth!.Value,
                AvatarUrl = _defaultAvatarUrl,
                Address = request.Address,
                IdentityNo = request.IdentityNo,
                Email = request.Email,
                Phone = request.Phone,
                Status = PendingStatus,
                HasPassword = false,
                PreviousStatus = null,
                PreviousBranchId = null,
                DeletedAt = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _userRepo.Create(createdUser);
            await _userRepo.SaveChangesAsync();

            response = new CreateAdminUserResponse
            {
                UserId = createdUser.UserId,
                RoleId = createdUser.RoleId,
                BranchId = createdUser.BranchId.Value,
                FullName = createdUser.FullName,
                IdentityNo = createdUser.IdentityNo,
                Email = createdUser.Email,
                Phone = createdUser.Phone,
                Status = createdUser.Status,
                CreatedAt = ConvertToVietnamTime(createdUser.CreatedAt),
                UpdatedAt = ConvertToVietnamTime(createdUser.UpdatedAt)
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

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "CREATE_ADMIN_USER",
                tableName: "users",
                recordId: createdUser.UserId.ToString(),
                oldValue: null,
                newValue: new
                {
                    createdUser.UserId,
                    createdUser.FullName,
                    createdUser.Email,
                    createdUser.Phone,
                    IdentityNo = IdentityNoEncryption.Mask(createdUser.IdentityNo),
                    createdUser.RoleId,
                    RoleName = role.RoleName,
                    createdUser.BranchId,
                    createdUser.Status
                });
        }

        return response;
    }

    public async Task<UpdateAdminUserResponse> UpdateAdminUserAsync(
        Guid userId,
        UpdateAdminUserRequest request,
        Guid? adminUserId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        NormalizeAndValidateUpdateRequest(request);

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        if (user.DeletedAt.HasValue)
            throw new InvalidOperationException("Deleted accounts cannot be updated.");

        if (!user.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase) &&
            !user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only active or pending accounts can be updated.");

        var roleName = user.Role.RoleName.Trim().ToUpperInvariant();
        var isAdmin = roleName == "ADMIN";
        if (roleName is not (ManagerRole or AccountantRole or "ADMIN"))
            throw new ArgumentException("Only Admin, Manager, or Accountant accounts can be updated by this endpoint.");

        if (!isAdmin && !request.BranchId.HasValue)
        {
            throw new ArgumentException("BranchId is required for Manager and Accountant accounts.");
        }

        var targetRole = await _userRepo.GetRoleByIdAsync(request.RoleId)
            ?? throw new ArgumentException("The specified role does not exist.");
        var targetRoleName = targetRole.RoleName.Trim().ToUpperInvariant();

        if (isAdmin)
        {
            request.RoleId = user.RoleId;
        }
        else if (targetRoleName is not (ManagerRole or AccountantRole))
        {
            throw new ArgumentException("Admin can only assign Manager or Accountant role.");
        }

        int? targetBranchId = request.BranchId;
        if (targetBranchId.HasValue)
        {
            if (!await _userRepo.ActiveBranchExistsAsync(targetBranchId.Value))
            {
                throw new ArgumentException("Target branch does not exist or is inactive.");
            }
        }

        if (!isAdmin && targetBranchId.HasValue)
        {
            var isBranchChanged = user.BranchId != targetBranchId.Value;
            var isRoleChanged = user.RoleId != request.RoleId;

            if (isBranchChanged || isRoleChanged)
            {
                var branchId = user.BranchId.GetValueOrDefault();
                if (roleName == AccountantRole && branchId > 0)
                {
                    var hasPendingPayroll = await _userRepo.HasPendingPayrollAsync(branchId);
                    if (hasPendingPayroll)
                    {
                        throw new InvalidOperationException("Cannot update Branch or Role because the Accountant has pending payroll processing in the current branch.");
                    }
                }

                // Check if another active or pending user already occupies the target role in the target branch
                var isRoleOccupied = await _userRepo.IsRoleOccupiedInBranchAsync(targetBranchId.Value, request.RoleId, userId);

                if (isRoleOccupied)
                {
                    throw new InvalidOperationException(
                        targetRoleName == ManagerRole
                            ? "This branch already has an active Manager."
                            : "This branch already has an active Accountant.");
                }
            }
        }

        var phoneOwner = await _userRepo.GetByPhoneAsync(request.Phone);
        if (phoneOwner is not null && phoneOwner.UserId != userId)
            ThrowIdentityConflict(phoneOwner, "Phone number");

        var identityOwner = await _userRepo.GetByIdentityNoAsync(request.IdentityNo);
        if (identityOwner is not null && identityOwner.UserId != userId)
            ThrowIdentityConflict(identityOwner, "Identity number");

        var emailOwner = await _userRepo.GetByEmailAsync(request.Email);
        if (emailOwner is not null && emailOwner.UserId != userId)
            throw new InvalidOperationException("Email is already in use by another user.");

        var oldSnapshot = new
        {
            user.FullName,
            user.Phone,
            IdentityNo = IdentityNoEncryption.Mask(user.IdentityNo),
            user.Gender,
            user.DateOfBirth,
            user.Address,
            user.Email,
            user.BranchId,
            user.RoleId,
            RoleName = user.Role.RoleName,
            user.AvatarUrl,
            user.Status
        };

        var changedFields = new List<string>();

        if (!string.Equals(user.FullName?.Trim(), request.FullName.Trim(), StringComparison.Ordinal))
            changedFields.Add("Full Name");

        if (!string.Equals(user.Phone?.Trim(), request.Phone.Trim(), StringComparison.Ordinal))
            changedFields.Add("Phone Number");

        if (!string.Equals(user.IdentityNo?.Trim(), request.IdentityNo.Trim(), StringComparison.Ordinal))
            changedFields.Add("Citizen ID");

        if (!string.Equals(user.Gender?.Trim(), request.Gender.Trim(), StringComparison.OrdinalIgnoreCase))
            changedFields.Add("Gender");

        if (user.DateOfBirth != request.DateOfBirth)
            changedFields.Add("Date of Birth");

        if (!string.Equals(user.Address?.Trim(), request.Address.Trim(), StringComparison.Ordinal))
            changedFields.Add("Address");

        if (!string.Equals(user.Email?.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            changedFields.Add("Email");

        if (user.BranchId != request.BranchId)
            changedFields.Add("Branch");

        if (user.RoleId != request.RoleId)
            changedFields.Add("Role");

        if (request.AvatarFile is not null && request.AvatarFile.Length > 0)
        {
            user.AvatarUrl = await _cloudinaryService.UploadAvatarAsync(userId, request.AvatarFile);
            changedFields.Add("Avatar");
        }

        var originalEmail = user.Email;
        var emailChanged = !string.Equals(originalEmail, request.Email, StringComparison.OrdinalIgnoreCase);

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.IdentityNo = request.IdentityNo;
        _userRepo.MarkIdentityNoModified(user);
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth!.Value;
        user.Address = request.Address;
        user.Email = request.Email;
        user.BranchId = request.BranchId;
        user.RoleId = request.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        NotificationEntity? notification = null;
        if (changedFields.Count > 0)
        {
            var createdBy = adminUserId.GetValueOrDefault();
            if (createdBy == Guid.Empty)
            {
                createdBy = await _userRepo.GetFirstActiveAdminIdAsync();

                if (createdBy == Guid.Empty)
                    createdBy = userId;
            }

            var fieldsText = string.Join(", ", changedFields);
            var now = DateTime.UtcNow;
            notification = new NotificationEntity
            {
                Title = "Profile Information Updated",
                Content = changedFields.Count == 1
                    ? $"Your profile has been updated by the administrator. Modified field: {fieldsText}."
                    : $"Your profile has been updated by the administrator. Modified fields: {fieldsText}.",
                NotificationType = "SYSTEM",
                CreatedBy = createdBy,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                TargetType = isAdmin ? "All" : "Custom",
                TargetBranchId = null,
                Channels = new List<string> { "InApp" },
                TargetRoles = isAdmin ? new List<string>() : new List<string> { targetRoleName }
            };

            notification.UserNotifications.Add(new UserNotification
            {
                UserId = userId,
                IsRead = false,
                IsDeleted = false,
                CreatedAt = now
            });

            _userRepo.AddNotification(notification);
        }

        using var transaction = await _userRepo.BeginTransactionAsync();
        try
        {
            await _userRepo.SaveChangesAsync();

            if (emailChanged)
            {
                await _authService.UpdateEmailAsync(userId, request.Email);
            }

            await transaction.CommitAsync();
            _memoryCache?.Remove(WorkShiftCacheKeys.UserRole(userId));

            if (notification is not null)
            {
                try
                {
                    await _notificationRealtimePublisher.PublishAsync(userId, new RealtimeNotificationResponse
                    {
                        NotificationId = notification.NotificationId,
                        Title = notification.Title,
                        Message = notification.Content,
                        CreatedAt = notification.CreatedAt
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish realtime notification to user {UserId}", userId);
                }
            }

            try
            {
                await _adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new { action = "UPDATED", userId });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish admin data changed event for user {UserId}", userId);
            }

            if (emailChanged && user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var setupLink = await _authService.ResendSetupLinkAsync(user.UserId, request.Email);
                    if (setupLink is not null)
                    {
                        await _accountSetupEmailSender.SendAsync(request.Email, setupLink);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-resend setup email to new address {Email} for pending user {UserId}", request.Email, userId);
                }
            }
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            var innerMessage = exception.InnerException?.Message ?? "";
            _logger.LogError(exception, "Database error updating user {UserId}. Inner: {InnerMessage}", userId, innerMessage);
            if (innerMessage.Contains("23503")) // Foreign key violation
            {
                throw new ArgumentException("The provided BranchId or RoleId does not exist.");
            }
            if (innerMessage.Contains("branch", StringComparison.OrdinalIgnoreCase) && innerMessage.Contains("role", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This branch already has an assigned user with this role.");
            }
            if (innerMessage.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Phone number is already in use by another user.");
            }
            if (innerMessage.Contains("identity", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Identity number is already in use by another user.");
            }
            if (innerMessage.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Email is already in use by another user.");
            }
            if (innerMessage.Contains("23505")) // PostgreSQL unique_violation
            {
                throw new InvalidOperationException("The phone number, identity number, or email is already in use.", exception);
            }
            throw new InvalidOperationException($"Unable to update account due to a system constraint: {exception.InnerException?.Message ?? exception.Message}", exception);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Reload the user with Role and Branch included to get RoleName and BranchName
        await _userRepo.LoadRoleAndBranchAsync(user);

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "UPDATE_ADMIN_USER",
                tableName: "users",
                recordId: user.UserId.ToString(),
                oldValue: oldSnapshot,
                newValue: new
                {
                    user.FullName,
                    user.Phone,
                    IdentityNo = IdentityNoEncryption.Mask(user.IdentityNo),
                    user.Gender,
                    user.DateOfBirth,
                    user.Address,
                    user.Email,
                    user.BranchId,
                    BranchName = user.Branch?.BranchName,
                    user.RoleId,
                    RoleName = user.Role.RoleName,
                    user.AvatarUrl,
                    user.Status,
                    ChangedFields = changedFields
                },
                actorUserId: adminUserId);
        }

        return new UpdateAdminUserResponse
        {
            UserId = user.UserId,
            AvatarUrl = user.AvatarUrl,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            IdentityNo = user.IdentityNo,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            RoleId = user.RoleId,
            RoleName = user.Role.RoleName,
            BranchId = user.BranchId,
            BranchName = user.Branch?.BranchName,
            Status = user.Status,
            UpdatedAt = ConvertToVietnamTime(user.UpdatedAt)
        };
    }

    public async Task ResendSetupLinkAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        if (user.DeletedAt is not null)
            throw new InvalidOperationException("Deleted accounts cannot receive invitations.");

        if (!user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only accounts pending activation can receive another invitation.");

        var setupLink = await _authService.ResendSetupLinkAsync(user.UserId, user.Email);
        if (setupLink is not null)
            await _accountSetupEmailSender.SendAsync(user.Email, setupLink);
    }

    public async Task SoftDeleteAdminUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        var roleName = user.Role.RoleName.Trim().ToUpperInvariant();
        if (roleName is not (ManagerRole or AccountantRole))
            throw new ArgumentException("Only Manager or Accountant accounts can be deleted by this endpoint.");

        if (user.DeletedAt.HasValue)
            throw new InvalidOperationException("The account has already been deleted.");

        if (user.Status.Equals(InactiveStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Inactive accounts cannot be deleted.");

        if (!user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase) &&
            !user.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The current account status does not allow deletion.");

        if (user.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            var branchId = user.BranchId.GetValueOrDefault();
            if (roleName == AccountantRole && branchId > 0)
            {
                var hasPendingPayroll = await _userRepo.HasPendingPayrollAsync(branchId);
                if (hasPendingPayroll)
                {
                    throw new InvalidOperationException("Cannot delete because the Accountant has pending payroll processing in the current branch.");
                }
            }
        }

        var oldSnapshot = new
        {
            user.Status,
            user.PreviousStatus,
            user.BranchId,
            user.PreviousBranchId,
            user.DeletedAt,
            RoleName = user.Role.RoleName
        };

        if (user.Status.Equals(PendingStatus, StringComparison.OrdinalIgnoreCase) ||
            user.Status.Equals(ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl != _defaultAvatarUrl)
            {
                try
                {
                    await _cloudinaryService.DeleteImageAsync($"account/{userId}/avatar");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete avatar from Cloudinary for user {UserId}", userId);
                }
                user.AvatarUrl = _defaultAvatarUrl;
            }
        }

        await _authService.BanUserAsync(user.UserId);
        try
        {
            var now = DateTime.UtcNow;
            user.PreviousStatus = user.Status;
            user.PreviousBranchId = user.BranchId;
            user.BranchId = null;
            user.Status = InactiveStatus;
            user.DeletedAt = now;
            user.UpdatedAt = now;
            await _userRepo.SaveChangesAsync();
            _memoryCache?.Remove(WorkShiftCacheKeys.UserRole(userId));
        }
        catch
        {
            try
            {
                await _authService.UnbanUserAsync(user.UserId);
            }
            catch (Exception rollbackException)
            {
                _logger.LogCritical(
                    rollbackException,
                    "Unable to unban Supabase Auth user {UserId} after soft-delete database failure",
                    user.UserId);
            }

            throw;
        }

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "DEACTIVATE_ADMIN_USER",
                tableName: "users",
                recordId: user.UserId.ToString(),
                oldValue: oldSnapshot,
                newValue: new
                {
                    user.Status,
                    user.PreviousStatus,
                    user.BranchId,
                    user.PreviousBranchId,
                    user.DeletedAt,
                    RoleName = roleName
                });
        }
    }

    public async Task RestoreAdminUserAsync(Guid userId, RestoreAdminUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        if (request.BranchId.HasValue && request.BranchId.Value <= 0)
            throw new ArgumentException("BranchId must be greater than 0.");

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("No matching user profile was found.");

        var roleName = user.Role.RoleName.Trim().ToUpperInvariant();
        var isManager = roleName == ManagerRole;
        var isAccountant = roleName == AccountantRole;
        if (!isManager && !isAccountant)
            throw new ArgumentException("Only Manager or Accountant accounts can be restored by this endpoint.");

        if (!user.DeletedAt.HasValue)
            throw new InvalidOperationException("The account has not been deleted.");

        if (!user.Status.Equals(InactiveStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The deleted account must currently be inactive.");

        var restoredStatus = user.PreviousStatus?.Trim().ToUpperInvariant();
        if (restoredStatus is not (PendingStatus or ActiveStatus))
            throw new InvalidOperationException("The account does not have a valid previous status to restore.");

        var targetBranchId = request.BranchId ?? user.PreviousBranchId
            ?? throw new InvalidOperationException(
                "No branch is available for restoring this account.");
        if (!await _userRepo.ActiveBranchExistsAsync(targetBranchId))
            throw new InvalidOperationException("The selected branch does not exist or is inactive.");

        if (await _userRepo.BranchHasRoleAsync(targetBranchId, user.RoleId))
        {
            throw new InvalidOperationException(
                isManager
                    ? "This branch already has a Manager."
                    : "This branch already has an Accountant.");
        }

        var oldSnapshot = new
        {
            user.Status,
            user.PreviousStatus,
            user.BranchId,
            user.PreviousBranchId,
            user.DeletedAt,
            RoleName = user.Role.RoleName
        };

        await _authService.UnbanUserAsync(user.UserId);
        try
        {
            user.BranchId = targetBranchId;
            user.PreviousBranchId = null;
            user.Status = restoredStatus;
            user.PreviousStatus = null;
            user.DeletedAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.SaveChangesAsync();
            _memoryCache?.Remove(WorkShiftCacheKeys.UserRole(userId));
        }
        catch (Exception exception)
        {
            try
            {
                await _authService.BanUserAsync(user.UserId);
            }
            catch (Exception rollbackException)
            {
                _logger.LogCritical(
                    rollbackException,
                    "Unable to re-ban Supabase Auth user {UserId} after restore database failure",
                    user.UserId);
            }

            if (exception is DbUpdateException)
            {
                throw new InvalidOperationException(
                    "The account could not be restored because its branch role is no longer available.",
                    exception);
            }

            throw;
        }

        if (_auditLogHelper != null)
        {
            await _auditLogHelper.LogAsync(
                action: "RESTORE_ADMIN_USER",
                tableName: "users",
                recordId: user.UserId.ToString(),
                oldValue: oldSnapshot,
                newValue: new
                {
                    user.Status,
                    user.PreviousStatus,
                    user.BranchId,
                    user.PreviousBranchId,
                    user.DeletedAt,
                    RoleName = roleName
                });
        }
    }

    private static DateTimeOffset ConvertToVietnamTime(DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utcDateTime), VietnamTimeZone);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
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
        if (!Regex.IsMatch(request.Phone, @"^(03[2-9]|05[2689]|07[06-9]|08[1-689]|09[0-46-9])\d{7}$"))
            throw new ArgumentException("Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (032-039, 052, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090-094, 096-099).");
        if (!Regex.IsMatch(request.IdentityNo, IdentityNoPattern))
            throw new ArgumentException("Identity number is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.");
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

    private static void NormalizeAndValidateUpdateRequest(UpdateAdminUserRequest request)
    {
        request.FullName = request.FullName.Trim();
        request.Phone = request.Phone.Trim();
        request.IdentityNo = request.IdentityNo.Trim();
        request.Gender = request.Gender.Trim().ToUpperInvariant();
        request.Address = request.Address.Trim();

        if (request.FullName.Length is 0 or > 150)
            throw new ArgumentException("Full name must contain between 1 and 150 characters.");
        if (!Regex.IsMatch(request.Phone, @"^(03[2-9]|05[2689]|07[06-9]|08[1-689]|09[0-46-9])\d{7}$"))
            throw new ArgumentException("Phone number must contain 10 digits and use a valid Vietnamese mobile prefix (032-039, 052, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090-094, 096-099).");
        if (!Regex.IsMatch(request.IdentityNo, IdentityNoPattern))
            throw new ArgumentException("Identity number is invalid. It must contain 9 or 12 digits starting with a valid Vietnam provincial code.");
        if (request.Gender is not ("MALE" or "FEMALE" or "OTHER"))
            throw new ArgumentException("Gender must be MALE, FEMALE, or OTHER.");
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
