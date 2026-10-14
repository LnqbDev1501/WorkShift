using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class AdminEmployeeRequestHistoryTests
{
    private readonly Mock<IAdminEmployeeRequestRepo> _repoMock;
    private readonly AdminEmployeeRequestService _service;

    public AdminEmployeeRequestHistoryTests()
    {
        _repoMock = new Mock<IAdminEmployeeRequestRepo>();
        _service = new AdminEmployeeRequestService(_repoMock.Object);
    }

    [Fact]
    public async Task AdminCanViewHistoryAcrossDifferentBranches_WithoutBranchRestriction()
    {
        // Arrange: 2 requests in different branches
        var branch1Logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 1,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = "101",
                UserId = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
                NewValue = JsonSerializer.Serialize(new { Status = "PENDING", Reason = "Nghỉ phép cá nhân" }),
                User = new User { FullName = "Staff Branch 1", Role = new Role { RoleName = "STAFF" } }
            }
        };

        var branch2Logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 2,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = "202",
                UserId = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc),
                NewValue = JsonSerializer.Serialize(new { Status = "PENDING", Reason = "Đi muộn vì kẹt xe" }),
                User = new User { FullName = "Staff Branch 2", Role = new Role { RoleName = "STAFF" } }
            }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch1Logs);
        _repoMock.Setup(r => r.GetHistoryAsync(202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch2Logs);

        // Act
        var result1 = await _service.GetHistoryAsync(101);
        var result2 = await _service.GetHistoryAsync(202);

        // Assert
        Assert.Single(result1);
        Assert.Equal("SUBMIT", result1[0].Action);
        Assert.Equal("Staff Branch 1", result1[0].PerformedByName);
        Assert.Equal("Nghỉ phép cá nhân", result1[0].ReviewNote);

        Assert.Single(result2);
        Assert.Equal("SUBMIT", result2[0].Action);
        Assert.Equal("Staff Branch 2", result2[0].PerformedByName);
        Assert.Equal("Đi muộn vì kẹt xe", result2[0].ReviewNote);
    }

    [Fact]
    public async Task GetHistory_WhenRequestDoesNotExist_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetHistoryAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Request not found."));

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetHistoryAsync(999));

        Assert.Equal("Request not found.", ex.Message);
    }

    [Fact]
    public async Task GetHistory_WhenRequestTypeIsNotSupported_ThrowsKeyNotFoundException()
    {
        // Repo checks RequestType in (LEAVE, LATE_ARRIVAL, EARLY_LEAVE) and throws KeyNotFoundException if outside scope
        _repoMock.Setup(r => r.GetHistoryAsync(888, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Request not found."));

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetHistoryAsync(888));

        Assert.Equal("Request not found.", ex.Message);
    }

    [Fact]
    public async Task GetHistory_WhenRequestIdIsZeroOrNegative_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetHistoryAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetHistoryAsync(-5));
    }

    [Fact]
    public async Task GetHistory_WhenRequestExistsButNoAuditLogs_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetHistoryAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());

        var result = await _service.GetHistoryAsync(100);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistory_MapsAllActionsAndStatusTransitionsAccurately()
    {
        var managerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 1,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = "50",
                UserId = staffId,
                CreatedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
                OldValue = null,
                NewValue = JsonSerializer.Serialize(new { Status = "PENDING", Reason = "Nghỉ ốm" }),
                User = new User { FullName = "Nguyen Van Staff", Role = new Role { RoleName = "STAFF" } }
            },
            new()
            {
                AuditLogId = 2,
                Action = "APPROVE",
                TableName = "employee_requests",
                RecordId = "50",
                UserId = managerId,
                CreatedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                OldValue = JsonSerializer.Serialize(new { Status = "PENDING" }),
                NewValue = JsonSerializer.Serialize(new { Status = "APPROVED", ReviewNote = "Đã duyệt đơn nghỉ" }),
                User = new User { FullName = "Tran Thi Manager", Role = new Role { RoleName = "MANAGER" } }
            }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _service.GetHistoryAsync(50);

        Assert.Equal(2, result.Count);

        // First event: SUBMIT
        Assert.Equal(1, result[0].AuditLogId);
        Assert.Equal("SUBMIT", result[0].Action);
        Assert.Equal(staffId, result[0].PerformedByUserId);
        Assert.Equal("Nguyen Van Staff", result[0].PerformedByName);
        Assert.Equal("STAFF", result[0].RoleName);
        Assert.Null(result[0].PreviousStatus);
        Assert.Equal("PENDING", result[0].NewStatus);
        Assert.Equal("Nghỉ ốm", result[0].ReviewNote);

        // Second event: APPROVE
        Assert.Equal(2, result[1].AuditLogId);
        Assert.Equal("APPROVE", result[1].Action);
        Assert.Equal(managerId, result[1].PerformedByUserId);
        Assert.Equal("Tran Thi Manager", result[1].PerformedByName);
        Assert.Equal("MANAGER", result[1].RoleName);
        Assert.Equal("PENDING", result[1].PreviousStatus);
        Assert.Equal("APPROVED", result[1].NewStatus);
        Assert.Equal("Đã duyệt đơn nghỉ", result[1].ReviewNote);
    }

    [Fact]
    public async Task GetHistory_WhenUserDeletedOrMissing_HandlesSafelyWithoutException()
    {
        var logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 1,
                Action = "CANCEL",
                TableName = "employee_requests",
                RecordId = "75",
                UserId = null,
                CreatedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                OldValue = JsonSerializer.Serialize(new { Status = "PENDING" }),
                NewValue = JsonSerializer.Serialize(new { Status = "CANCELLED" }),
                User = null
            }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(75, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _service.GetHistoryAsync(75);

        Assert.Single(result);
        Assert.Null(result[0].PerformedByUserId);
        Assert.Null(result[0].PerformedByName);
        Assert.Null(result[0].RoleName);
        Assert.Equal("CANCEL", result[0].Action);
        Assert.Equal("PENDING", result[0].PreviousStatus);
        Assert.Equal("CANCELLED", result[0].NewStatus);
    }

    [Fact]
    public async Task GetHistory_PreservesStableChronologicalOrdering()
    {
        var logs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 10,
                Action = "SUBMIT",
                TableName = "employee_requests",
                RecordId = "60",
                CreatedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
                NewValue = JsonSerializer.Serialize(new { Status = "PENDING" })
            },
            new()
            {
                AuditLogId = 11,
                Action = "APPROVE",
                TableName = "employee_requests",
                RecordId = "60",
                CreatedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
                OldValue = JsonSerializer.Serialize(new { Status = "PENDING" }),
                NewValue = JsonSerializer.Serialize(new { Status = "APPROVED" })
            }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _service.GetHistoryAsync(60);

        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].AuditLogId);
        Assert.Equal("SUBMIT", result[0].Action);
        Assert.Equal(11, result[1].AuditLogId);
        Assert.Equal("APPROVE", result[1].Action);
        Assert.True(result[0].PerformedAt <= result[1].PerformedAt);
    }
}
