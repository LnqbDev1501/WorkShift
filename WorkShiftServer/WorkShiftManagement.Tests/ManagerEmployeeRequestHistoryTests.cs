using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using Xunit;

namespace WorkShiftManagement.Tests;

public class ManagerEmployeeRequestHistoryTests
{
    private readonly Mock<IManagerEmployeeRequestRepo> _repoMock;
    private readonly ManagerEmployeeRequestService _service;

    public ManagerEmployeeRequestHistoryTests()
    {
        _repoMock = new Mock<IManagerEmployeeRequestRepo>();
        _service = new ManagerEmployeeRequestService(_repoMock.Object);
    }

    private static User CreateActiveManager(Guid managerId, int branchId)
    {
        return new User
        {
            UserId = managerId,
            FullName = "Branch Manager",
            Status = "ACTIVE",
            BranchId = branchId,
            Role = new Role { RoleName = "MANAGER", Status = "ACTIVE" },
            Branch = new Branch { BranchId = branchId, BranchName = "Branch 1", Status = "ACTIVE" }
        };
    }

    [Fact]
    public async Task ManagerCanViewHistoryOfRequestInTheirOwnBranch()
    {
        var managerId = Guid.NewGuid();
        var branchId = 10;
        var requestId = 101;

        _repoMock.Setup(r => r.GetActiveManagerAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveManager(managerId, branchId));

        var logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 1,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = requestId.ToString(),
                UserId = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc),
                OldValue = null,
                NewValue = JsonSerializer.Serialize(new { Status = "PENDING", Reason = "Nghỉ phép thường niên" }),
                User = new User { FullName = "Staff X", Role = new Role { RoleName = "STAFF" } }
            }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(requestId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _service.GetHistoryAsync(managerId, requestId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].AuditLogId);
        Assert.Equal("SUBMIT", result[0].Action);
        Assert.Equal("Staff X", result[0].PerformedByName);
        Assert.Equal("Nghỉ phép thường niên", result[0].ReviewNote);
        Assert.Equal("PENDING", result[0].NewStatus);
    }

    [Fact]
    public async Task ManagerCannotViewHistoryOfRequestInAnotherBranch_ThrowsKeyNotFoundException()
    {
        var managerId = Guid.NewGuid();
        var managerBranchId = 10;
        var otherBranchRequestId = 202;

        _repoMock.Setup(r => r.GetActiveManagerAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveManager(managerId, managerBranchId));

        // Repo enforces branchId: if request is in branch 20 instead of 10, repo throws KeyNotFoundException
        _repoMock.Setup(r => r.GetHistoryAsync(otherBranchRequestId, managerBranchId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Request not found in this branch."));

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetHistoryAsync(managerId, otherBranchRequestId));

        Assert.Equal("Request not found in this branch.", ex.Message);
    }

    [Fact]
    public async Task Manager_WhenRequestTypeIsNotSupported_ThrowsKeyNotFoundException()
    {
        var managerId = Guid.NewGuid();
        var managerBranchId = 10;
        var unsupportedTypeRequestId = 303;

        _repoMock.Setup(r => r.GetActiveManagerAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveManager(managerId, managerBranchId));

        // Repo checks RequestType in (LEAVE, LATE_ARRIVAL, EARLY_LEAVE) and throws KeyNotFoundException if outside scope
        _repoMock.Setup(r => r.GetHistoryAsync(unsupportedTypeRequestId, managerBranchId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Request not found in this branch."));

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetHistoryAsync(managerId, unsupportedTypeRequestId));

        Assert.Equal("Request not found in this branch.", ex.Message);
    }

    [Fact]
    public async Task Manager_WhenManagerNotAssignedToActiveBranch_ThrowsUnauthorizedAccessException()
    {
        var managerId = Guid.NewGuid();
        var manager = new User
        {
            UserId = managerId,
            FullName = "Unassigned Manager",
            Status = "ACTIVE",
            BranchId = null,
            Role = new Role { RoleName = "MANAGER", Status = "ACTIVE" },
            Branch = null
        };

        _repoMock.Setup(r => r.GetActiveManagerAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetHistoryAsync(managerId, 50));

        Assert.Contains("active branch", ex.Message);
    }

    [Fact]
    public async Task Manager_WhenManagerAccountDeactivated_ThrowsUnauthorizedAccessException()
    {
        var managerId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetActiveManagerAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetHistoryAsync(managerId, 50));

        Assert.Contains("deactivated", ex.Message);
    }
}
