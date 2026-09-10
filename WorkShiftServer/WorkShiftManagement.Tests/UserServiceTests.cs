using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepo> _userRepoMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAccountSetupEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepo>();
        _authServiceMock = new Mock<IAuthService>();
        _emailSenderMock = new Mock<IAccountSetupEmailSender>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["Cloudinary:DefaultAvatarUrl"])
            .Returns("https://example.com/avatar.jpg");

        _userService = new UserService(
            _userRepoMock.Object,
            _authServiceMock.Object,
            _emailSenderMock.Object,
            _loggerMock.Object,
            _configMock.Object);
    }

    private static CreateAdminUserRequest CreateValidRequest()
    {
        return new CreateAdminUserRequest
        {
            FullName = "Truc Tester",
            Email = "truchttce181115@fpt.edu.vn",
            Phone = "0901234567",
            IdentityNo = "189304017654",
            RoleId = 2, // Manager
            BranchId = 2,
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(2004, 9, 8),
            Address = "HA NOI"
        };
    }

    private void SetupValidDependencies(CreateAdminUserRequest request, Guid authUserId)
    {
        _userRepoMock.Setup(r => r.GetActiveRoleByIdAsync(request.RoleId))
            .ReturnsAsync(new Role { RoleId = request.RoleId, RoleName = "MANAGER" });

        _userRepoMock.Setup(r => r.ActiveBranchExistsAsync(request.BranchId!.Value))
            .ReturnsAsync(true);

        _userRepoMock.Setup(r => r.BranchHasRoleAsync(request.BranchId!.Value, request.RoleId))
            .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email.ToLowerInvariant()))
            .ReturnsAsync((User?)null);

        _userRepoMock.Setup(r => r.GetByPhoneAsync(request.Phone))
            .ReturnsAsync((User?)null);

        _userRepoMock.Setup(r => r.GetByIdentityNoAsync(request.IdentityNo))
            .ReturnsAsync((User?)null);

        _authServiceMock.Setup(a => a.CreateUserAndSendInviteAsync(request.Email.ToLowerInvariant()))
            .ReturnsAsync(authUserId);
    }

    [Fact]
    public async Task UTCID01_CreateAdminUser_ValidInput_Returns201CreatedStatusPending()
    {
        // Arrange
        var request = CreateValidRequest();
        var authUserId = Guid.NewGuid();
        SetupValidDependencies(request, authUserId);

        // Act
        var response = await _userService.CreateAdminUserAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(authUserId, response.UserId);
        Assert.Equal("PENDING", response.Status);
        Assert.Equal(request.Email.ToLowerInvariant(), response.Email);
        Assert.Equal(request.FullName, response.FullName);

        _userRepoMock.Verify(r => r.Create(It.Is<User>(u =>
            u.UserId == authUserId &&
            u.FullName == request.FullName &&
            u.Email == request.Email.ToLowerInvariant() &&
            u.Status == "PENDING"
        )), Times.Once);

        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UTCID02_CreateAdminUser_BranchAlreadyHasManager_ThrowsInvalidOperationException()
    {
        // Arrange: Branch 2 already has a Manager assigned
        var request = CreateValidRequest();

        _userRepoMock.Setup(r => r.GetActiveRoleByIdAsync(request.RoleId))
            .ReturnsAsync(new Role { RoleId = request.RoleId, RoleName = "MANAGER" });

        _userRepoMock.Setup(r => r.ActiveBranchExistsAsync(request.BranchId!.Value))
            .ReturnsAsync(true);

        _userRepoMock.Setup(r => r.BranchHasRoleAsync(request.BranchId!.Value, request.RoleId))
            .ReturnsAsync(true); // Manager already exists

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Equal("This branch already has a Manager.", exception.Message);
    }

    [Fact]
    public async Task UTCID03_CreateAdminUser_PhoneBelongsToActiveAccount_ThrowsInvalidOperationException()
    {
        // Arrange: Phone number belongs to an active user
        var request = CreateValidRequest();

        _userRepoMock.Setup(r => r.GetActiveRoleByIdAsync(request.RoleId))
            .ReturnsAsync(new Role { RoleId = request.RoleId, RoleName = "MANAGER" });

        _userRepoMock.Setup(r => r.ActiveBranchExistsAsync(request.BranchId!.Value))
            .ReturnsAsync(true);

        _userRepoMock.Setup(r => r.BranchHasRoleAsync(request.BranchId!.Value, request.RoleId))
            .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email.ToLowerInvariant()))
            .ReturnsAsync((User?)null);

        _userRepoMock.Setup(r => r.GetByPhoneAsync(request.Phone))
            .ReturnsAsync(new User { UserId = Guid.NewGuid(), Phone = request.Phone, Status = "ACTIVE" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Contains("Phone number is already in use", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID04_CreateAdminUser_EmptyFullName_ThrowsArgumentException()
    {
        // Arrange: Empty Full Name
        var request = CreateValidRequest();
        request.FullName = "   ";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Contains("Full name must contain between 1 and 150 characters", exception.Message);
    }

    [Fact]
    public async Task UTCID05_CreateAdminUser_InvalidEmailFormat_ThrowsArgumentException()
    {
        // Arrange: Email missing @ symbol
        var request = CreateValidRequest();
        request.Email = "newuser_fpt.edu.vn";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Equal("Email format is invalid.", exception.Message);
    }

    [Fact]
    public async Task UTCID06_CreateAdminUser_InvalidPhoneLength_ThrowsArgumentException()
    {
        // Arrange: Phone number too short
        var request = CreateValidRequest();
        request.Phone = "012345";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Contains("Phone number must contain 10 digits", exception.Message);
    }

    [Fact]
    public async Task UTCID07_CreateAdminUser_InvalidIdentityNoLength_ThrowsArgumentException()
    {
        // Arrange: IdentityNo too short (6 digits)
        var request = CreateValidRequest();
        request.IdentityNo = "012345";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Equal("Identity number must contain either 9 or 12 digits.", exception.Message);
    }

    [Fact]
    public async Task UTCID08_CreateAdminUser_UnderageUserLessThan18YearsOld_ThrowsArgumentException()
    {
        // Arrange: Underage user born in 2020
        var request = CreateValidRequest();
        request.DateOfBirth = new DateOnly(2020, 1, 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.CreateAdminUserAsync(request));

        Assert.Equal("User must be at least 18 years old.", exception.Message);
    }

    [Fact]
    public async Task UTCID09_CreateAdminUser_Boundary150CharsFullName_Returns201CreatedStatusPending()
    {
        // Arrange: Max boundary full name (150 chars)
        var request = CreateValidRequest();
        request.FullName = new string('A', 150);

        var authUserId = Guid.NewGuid();
        SetupValidDependencies(request, authUserId);

        // Act
        var response = await _userService.CreateAdminUserAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(authUserId, response.UserId);
        Assert.Equal(150, response.FullName.Length);
        Assert.Equal("PENDING", response.Status);
    }
}
