using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftServer.API.Controllers.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class AdminEmploymentContractHistoryTests
{
    private readonly Mock<IEmploymentContractRepo> _repoMock;
    private readonly AdminEmploymentContractService _service;
    private readonly Mock<ILogger<AdminEmploymentContractController>> _controllerLoggerMock;

    public AdminEmploymentContractHistoryTests()
    {
        _repoMock = new Mock<IEmploymentContractRepo>();
        _service = new AdminEmploymentContractService(_repoMock.Object);
        _controllerLoggerMock = new Mock<ILogger<AdminEmploymentContractController>>();
    }

    [Fact]
    public async Task AdminCanViewHistoryAcrossDifferentBranches_WithoutBranchRestriction()
    {
        // Branch 1 contract
        var branch1Contract = new EmploymentContract
        {
            ContractId = 101,
            ContractNumber = "EC-BR1-001",
            Status = "ACTIVE",
            BranchId = 1,
            BranchNameSnapshot = "Chi nhánh Quận 1",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Nguyen Van A"
        };

        // Branch 2 contract
        var branch2Contract = new EmploymentContract
        {
            ContractId = 202,
            ContractNumber = "EC-BR2-002",
            Status = "CANCELLED",
            BranchId = 2,
            BranchNameSnapshot = "Chi nhánh Quận 2",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Tran Thi B"
        };

        _repoMock.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch1Contract);
        _repoMock.Setup(r => r.GetByIdAsync(202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch2Contract);

        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(101, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));
        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(202, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        // Act: Admin retrieves history for both contracts in different branches
        var result1 = await _service.GetContractHistoryAsync(101);
        var result2 = await _service.GetContractHistoryAsync(202);

        // Assert: Both succeed without throwing branch unauthorized exceptions
        Assert.NotNull(result1);
        Assert.Equal(101, result1.ContractId);
        Assert.Equal(1, result1.BranchId);
        Assert.Equal("Chi nhánh Quận 1", result1.BranchName);

        Assert.NotNull(result2);
        Assert.Equal(202, result2.ContractId);
        Assert.Equal(2, result2.BranchId);
        Assert.Equal("Chi nhánh Quận 2", result2.BranchName);
    }

    [Fact]
    public async Task GetContractHistory_WhenContractDoesNotExist_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmploymentContract?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetContractHistoryAsync(999));

        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task GetContractHistory_WhenContractIdIsInvalid_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetContractHistoryAsync(0));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetContractHistoryAsync(-5));
    }

    [Fact]
    public async Task GetContractHistory_IncludesAutoCancelEvent_WithCancelledStatusAndCorrectReason()
    {
        var contract = new EmploymentContract
        {
            ContractId = 55,
            ContractNumber = "EC-OVERDUE-055",
            Status = "CANCELLED",
            CancellationReason = "NOT_ACCEPTED_BY_START_DATE",
            BranchId = 3,
            BranchNameSnapshot = "Chi nhánh Hà Nội",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Le Van Overdue"
        };

        var now = DateTime.UtcNow;
        var autoCancelLog = new AuditLog
        {
            AuditLogId = 777,
            UserId = null, // System action
            Action = "AUTO_CANCEL_CONTRACT",
            TableName = "employment_contracts",
            RecordId = "55",
            OldValue = JsonSerializer.Serialize(new { fromStatus = "PENDING_STAFF" }),
            NewValue = JsonSerializer.Serialize(new
            {
                fromStatus = "PENDING_STAFF",
                toStatus = "CANCELLED",
                reason = "NOT_ACCEPTED_BY_START_DATE",
                cancelledAt = now,
                note = "Hệ thống tự động hủy hợp đồng do hết ngày bắt đầu mà nhân viên chưa chấp nhận."
            }),
            CreatedAt = now
        };

        _repoMock.Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(55, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog> { autoCancelLog }, 1));

        var result = await _service.GetContractHistoryAsync(55);

        Assert.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items[0];
        Assert.Equal("AUTO_CANCEL_CONTRACT", item.Action);
        Assert.Equal("Hệ thống tự động hủy hợp đồng do quá hạn chấp nhận", item.ActionTitle);
        Assert.Equal("PENDING_STAFF", item.FromStatus);
        Assert.Equal("CANCELLED", item.ToStatus);
        Assert.NotEqual("TERMINATED", item.ToStatus); // Ensure NOT displayed as TERMINATED
        Assert.Equal("NOT_ACCEPTED_BY_START_DATE", item.Reason);
        Assert.Contains("Hệ thống tự động hủy", item.Note);

        // System accountability check
        Assert.Equal("Hệ thống", item.ActorName);
        Assert.Equal("SYSTEM", item.ActorRole);
        Assert.Null(item.ActorId);
    }

    [Fact]
    public void Controller_HasAdminAuthorizationPolicy_RestrictsNonAdminAccess()
    {
        var controllerType = typeof(AdminEmploymentContractController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttr);
        Assert.Equal("Admin", authAttr.Policy);

        // History endpoint has HttpGet and route {contractId:int}/history
        var historyMethod = controllerType.GetMethod("GetContractHistory");
        Assert.NotNull(historyMethod);

        var getAttr = historyMethod.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(getAttr);
        Assert.Equal("{contractId:int}/history", getAttr.Template);

        // Ensure there are no write/delete methods for history (Admin history is read-only)
        var writeHistoryMethods = controllerType.GetMethods()
            .Where(m => m.Name.Contains("History", StringComparison.OrdinalIgnoreCase) && m.Name != "GetContractHistory")
            .ToList();
        Assert.Empty(writeHistoryMethods);
    }

    [Fact]
    public async Task Controller_GetContractHistory_ReturnsOkResult_WhenFound()
    {
        var serviceMock = new Mock<IAdminEmploymentContractService>();
        var expectedResponse = new AdminContractHistoryResponse
        {
            ContractId = 12,
            ContractNumber = "EC-12",
            CurrentStatus = "ACTIVE",
            Items = new List<ContractHistoryEntryDto>()
        };

        serviceMock.Setup(s => s.GetContractHistoryAsync(12, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new AdminEmploymentContractController(serviceMock.Object, _controllerLoggerMock.Object);
        var response = await controller.GetContractHistory(12);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var history = Assert.IsType<AdminContractHistoryResponse>(okResult.Value);
        Assert.Equal(12, history.ContractId);
    }

    [Fact]
    public async Task Controller_GetContractHistory_ReturnsNotFound_WhenContractDoesNotExist()
    {
        var serviceMock = new Mock<IAdminEmploymentContractService>();
        serviceMock.Setup(s => s.GetContractHistoryAsync(999, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Contract not found"));

        var controller = new AdminEmploymentContractController(serviceMock.Object, _controllerLoggerMock.Object);
        var response = await controller.GetContractHistory(999);

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task Controller_GetContractHistory_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var serviceMock = new Mock<IAdminEmploymentContractService>();
        serviceMock.Setup(s => s.GetContractHistoryAsync(-1, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid contract id"));

        var controller = new AdminEmploymentContractController(serviceMock.Object, _controllerLoggerMock.Object);
        var response = await controller.GetContractHistory(-1);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }
}
