using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;
using WorkShiftManagement.DAL.Repositories.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class BranchServiceTests
{
    private readonly Mock<IBranchRepo> _branchRepoMock;
    private readonly Mock<IRegionRepo> _regionRepoMock;
    private readonly BranchService _branchService;

    public BranchServiceTests()
    {
        _branchRepoMock = new Mock<IBranchRepo>();
        _regionRepoMock = new Mock<IRegionRepo>();
        _branchService = new BranchService(_branchRepoMock.Object, _regionRepoMock.Object);
    }

    [Fact]
    public async Task Requirement1_CreateBranch_RegionInactive_ThrowsArgumentException()
    {
        // Arrange: Region exists but is INACTIVE (IsActive = false)
        var inactiveRegionId = 10;
        _regionRepoMock.Setup(r => r.GetByIdAsync(inactiveRegionId))
            .ReturnsAsync(new RegionDetailData
            {
                RegionId = inactiveRegionId,
                RegionCode = "REG01",
                RegionName = "Mien Bac",
                IsActive = false
            });

        var request = new CreateBranchRequest
        {
            BranchCode = "BR001",
            BranchName = "Chi Nhanh Ha Noi",
            RegionId = inactiveRegionId,
            Address = "123 Cau Giay, Ha Noi"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _branchService.CreateAsync(request));
        Assert.Contains("inactive region", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Requirement2_UpdateBranch_RegionInactive_ThrowsArgumentException()
    {
        // Arrange: Existing active branch
        var branchId = 1;
        var existingBranch = new Branch
        {
            BranchId = branchId,
            BranchCode = "BR001",
            BranchName = "Branch Current",
            RegionId = 5,
            Address = "Old Address",
            Status = "ACTIVE"
        };

        _branchRepoMock.Setup(b => b.GetEntityByIdAsync(branchId))
            .ReturnsAsync(existingBranch);

        // Target Region is INACTIVE
        var inactiveRegionId = 20;
        _regionRepoMock.Setup(r => r.GetByIdAsync(inactiveRegionId))
            .ReturnsAsync(new RegionDetailData
            {
                RegionId = inactiveRegionId,
                RegionCode = "REG02",
                RegionName = "Mien Nam",
                IsActive = false
            });

        var request = new UpdateBranchRequest
        {
            BranchName = "Branch Updated Name",
            RegionId = inactiveRegionId,
            Address = "New Address 123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _branchService.UpdateAsync(branchId, request));
        Assert.Contains("inactive region", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Requirement3_DeleteBranch_SoftDeletes_SetsStatusInactive_UserUnchanged()
    {
        // Arrange EF Core InMemory Context to test real SoftDelete logic
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var userId = Guid.NewGuid();

        using (var context = new AppDbContext(options))
        {
            var branch = new Branch
            {
                BranchId = 1,
                BranchCode = "BR001",
                BranchName = "Branch 1",
                RegionId = 1,
                Address = "123 Street",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var user = new User
            {
                UserId = userId,
                FullName = "Nguyen Van A",
                Gender = "Male",
                AvatarUrl = "http://avatar.jpg",
                Address = "123 Street",
                IdentityNo = "123456789",
                Email = "a@gmail.com",
                Phone = "0987654321",
                BranchId = 1,
                PreviousBranchId = null,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Branches.Add(branch);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repo = new BranchRepo(context);

            // Act
            var result = await repo.SoftDeleteAsync(1);

            // Assert
            Assert.True(result);
            var updatedBranch = await context.Branches.FindAsync(1);
            Assert.NotNull(updatedBranch);
            Assert.Equal("INACTIVE", updatedBranch.Status);

            // Verify User is intact & unchanged
            var updatedUser = await context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal(1, updatedUser.BranchId);
            Assert.Null(updatedUser.PreviousBranchId);
            Assert.Equal("ACTIVE", updatedUser.Status);
        }
    }

    [Fact]
    public async Task Requirement4_RestoreBranch_WhenRegionInactive_ThrowsArgumentException()
    {
        // Arrange: Branch details where RegionIsActive = false
        var branchId = 2;
        var inactiveRegionBranchDetail = new BranchDetailData
        {
            BranchId = branchId,
            BranchCode = "BR002",
            BranchName = "Branch HCM",
            RegionId = 10,
            RegionIsActive = false,
            Status = "INACTIVE"
        };

        _branchRepoMock.Setup(b => b.GetByIdAsync(branchId))
            .ReturnsAsync(inactiveRegionBranchDetail);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _branchService.RestoreAsync(branchId));
        Assert.Contains("region is inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Requirement5_GetBranchEmployees_UserWithPreviousBranchIdOnly_Excluded()
    {
        // Arrange EF Core InMemory Context to test BranchRepo employee filtering
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        using (var context = new AppDbContext(options))
        {
            var role = new Role { RoleId = 1, RoleName = "STAFF", Status = "ACTIVE" };
            context.Roles.Add(role);

            // User 1: currently in branch 10 (BranchId = 10)
            var activeEmployee = new User
            {
                UserId = user1Id,
                FullName = "Emp Active",
                Gender = "Male",
                AvatarUrl = "http://avatar1.jpg",
                Address = "Addr 1",
                IdentityNo = "ID1",
                Email = "active@test.com",
                Phone = "0123456789",
                RoleId = 1,
                Role = role,
                BranchId = 10,
                PreviousBranchId = null,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // User 2: previously in branch 10 (PreviousBranchId = 10), but currently unassigned (BranchId = null)
            var unassignedExEmployee = new User
            {
                UserId = user2Id,
                FullName = "Emp Transferred/Deleted",
                Gender = "Female",
                AvatarUrl = "http://avatar2.jpg",
                Address = "Addr 2",
                IdentityNo = "ID2",
                Email = "transferred@test.com",
                Phone = "0987654321",
                RoleId = 1,
                Role = role,
                BranchId = null,
                PreviousBranchId = 10,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // User 3: transferred to branch 20 (BranchId = 20, PreviousBranchId = 10)
            var transferredEmployee = new User
            {
                UserId = user3Id,
                FullName = "Emp In Branch 20",
                Gender = "Male",
                AvatarUrl = "http://avatar3.jpg",
                Address = "Addr 3",
                IdentityNo = "ID3",
                Email = "b20@test.com",
                Phone = "0112233445",
                RoleId = 1,
                Role = role,
                BranchId = 20,
                PreviousBranchId = 10,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(activeEmployee, unassignedExEmployee, transferredEmployee);
            await context.SaveChangesAsync();

            var repo = new BranchRepo(context);

            // Act: Query employees for branchId = 10
            var (items, totalCount) = await repo.GetEmployeesPagedAsync(10, null, null, null, 1, 10);

            // Assert: Only User 1 should be returned. User 2 and User 3 must NOT appear.
            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Equal(user1Id, items[0].UserId);
            Assert.Equal("Emp Active", items[0].FullName);
        }
    }
}
