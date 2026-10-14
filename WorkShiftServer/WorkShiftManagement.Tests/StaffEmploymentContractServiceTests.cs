using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class StaffEmploymentContractServiceTests
{
    private readonly Mock<IEmploymentContractRepo> _repoMock;
    private readonly Mock<ICloudinaryService> _cloudinaryMock;
    private readonly StaffEmploymentContractService _service;

    public StaffEmploymentContractServiceTests()
    {
        _repoMock = new Mock<IEmploymentContractRepo>();
        _cloudinaryMock = new Mock<ICloudinaryService>();
        _service = new StaffEmploymentContractService(_repoMock.Object, _cloudinaryMock.Object);
    }

    [Fact]
    public async Task SubmitChangeRequest_WhenContractNotFound_ThrowsKeyNotFoundException()
    {
        var staffUserId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmploymentContract?)null);

        var request = new SubmitContractChangeRequestRequest
        {
            Items = new List<ContractChangeRequestItemInputDto>
            {
                new() { FieldName = "EmployeePosition", ProposedValue = "Trưởng ca" }
            }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.SubmitChangeRequestAsync(staffUserId, 1, request));
    }

    [Fact]
    public async Task SubmitChangeRequest_WhenContractStatusCancelled_ThrowsInvalidOperationException()
    {
        var staffUserId = Guid.NewGuid();
        var contract = new EmploymentContract
        {
            ContractId = 1,
            UserId = staffUserId,
            Status = "CANCELLED"
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new SubmitContractChangeRequestRequest
        {
            Items = new List<ContractChangeRequestItemInputDto>
            {
                new() { FieldName = "EmployeePosition", ProposedValue = "Trưởng ca" }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitChangeRequestAsync(staffUserId, 1, request));
    }

    [Fact]
    public async Task SubmitChangeRequest_WhenDuplicateOpenRequestExists_ThrowsInvalidOperationException()
    {
        var staffUserId = Guid.NewGuid();
        var contract = new EmploymentContract
        {
            ContractId = 1,
            UserId = staffUserId,
            Status = "PENDING_STAFF"
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.GetOpenChangeRequestByContractIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContractChangeRequest { ChangeRequestId = 99, Status = "SUBMITTED" });

        var request = new SubmitContractChangeRequestRequest
        {
            Items = new List<ContractChangeRequestItemInputDto>
            {
                new() { FieldName = "EmployeePosition", ProposedValue = "Trưởng ca" }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitChangeRequestAsync(staffUserId, 1, request));
    }

    [Fact]
    public async Task SubmitChangeRequest_WhenDuplicateFieldNameInItems_ThrowsArgumentException()
    {
        var staffUserId = Guid.NewGuid();

        var request = new SubmitContractChangeRequestRequest
        {
            Items = new List<ContractChangeRequestItemInputDto>
            {
                new() { FieldName = "EmployeePosition", ProposedValue = "Trưởng ca" },
                new() { FieldName = "EmployeePosition", ProposedValue = "Nhân viên" }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SubmitChangeRequestAsync(staffUserId, 1, request));
    }

    [Fact]
    public async Task SubmitChangeRequest_WhenValid_CreatesRequestSuccessfully()
    {
        var staffUserId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-12345",
            UserId = staffUserId,
            Status = "PENDING_STAFF",
            CreatedBy = managerId,
            BranchId = 2,
            EmployeeFullNameSnapshot = "Nguyen Van A",
            StartDate = new DateOnly(2026, 11, 1),
            EmployeePositionSnapshot = "Thu ngân"
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.GetOpenChangeRequestByContractIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContractChangeRequest?)null);

        var request = new SubmitContractChangeRequestRequest
        {
            StaffComment = "Xin điều chỉnh vị trí và ngày bắt đầu",
            Items = new List<ContractChangeRequestItemInputDto>
            {
                new() { FieldName = "StartDate", ProposedValue = "2026-11-15", Reason = "Bận việc gia đình" },
                new() { FieldName = "EmployeePosition", ProposedValue = "Phục vụ", Reason = "Nguyện vọng cá nhân" }
            }
        };

        var response = await _service.SubmitChangeRequestAsync(staffUserId, 1, request);

        Assert.NotNull(response);
        Assert.Equal("SUBMITTED", response.Status);
        Assert.Equal(2, response.Items.Count);
        _repoMock.Verify(r => r.CreateChangeRequestAsync(It.IsAny<ContractChangeRequest>(), "PENDING_STAFF", It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptContract_WhenPastStartDate_ThrowsInvalidOperationException()
    {
        var staffUserId = Guid.NewGuid();
        var pastStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-2));
        var contract = new EmploymentContract
        {
            ContractId = 1,
            UserId = staffUserId,
            Status = "PENDING_STAFF",
            StartDate = pastStartDate
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AcceptContractAsync(staffUserId, 1));
        Assert.Contains("Hợp đồng đã quá ngày bắt đầu", ex.Message);
    }

    [Fact]
    public async Task AcceptContract_WhenNotPendingStaff_ThrowsInvalidOperationException()
    {
        var staffUserId = Guid.NewGuid();
        var futureStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(5));
        var contract = new EmploymentContract
        {
            ContractId = 1,
            UserId = staffUserId,
            Status = "ACTIVE",
            StartDate = futureStartDate
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AcceptContractAsync(staffUserId, 1));
    }

    [Fact]
    public async Task AcceptContract_WhenFutureStartDate_SetsStatusScheduled()
    {
        var staffUserId = Guid.NewGuid();
        var futureStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(5));
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-12345",
            UserId = staffUserId,
            Status = "PENDING_STAFF",
            StartDate = futureStartDate,
            EmployeeFullNameSnapshot = "Nguyen Van A"
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await _service.AcceptContractAsync(staffUserId, 1);

        _repoMock.Verify(r => r.StaffAcceptContractAsync(1, staffUserId, "SCHEDULED", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptContract_WhenExactSameDayAsStartDate_SetsStatusActive()
    {
        var staffUserId = Guid.NewGuid();
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-12345",
            UserId = staffUserId,
            Status = "PENDING_STAFF",
            StartDate = todayVn,
            EmployeeFullNameSnapshot = "Nguyen Van A"
        };

        _repoMock.Setup(r => r.GetStaffContractByIdAsync(1, staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await _service.AcceptContractAsync(staffUserId, 1);

        _repoMock.Verify(r => r.StaffAcceptContractAsync(1, staffUserId, "ACTIVE", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOverduePendingContracts_CallsRepoWithTodayVnDate()
    {
        _repoMock.Setup(r => r.CancelOverduePendingContractsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult { CancelledCount = 2 });

        var count = await _service.CancelOverduePendingContractsAsync();

        Assert.Equal(2, count);
        _repoMock.Verify(r => r.CancelOverduePendingContractsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStaffPdfFile_WhenCloudinaryDownloadFails_ThrowsException()
    {
        var staffUserId = Guid.NewGuid();
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-12345",
            UserId = staffUserId,
            Status = "PENDING_STAFF",
            ContractFilePath = "workshift/contracts/EC-12345"
        };

        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _cloudinaryMock.Setup(c => c.DownloadRawFileAsync("workshift/contracts/EC-12345"))
            .ThrowsAsync(new InvalidOperationException("Cloudinary network timeout"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetStaffPdfFileAsync(staffUserId, 1, false));
    }

    [Fact]
    public async Task CanStaffRegisterShift_DelegatesToRepoWithCorrectParams()
    {
        var staffUserId = Guid.NewGuid();
        var branchId = 3;
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        _repoMock.Setup(r => r.HasActiveContractForShiftRegistrationAsync(staffUserId, branchId, startUtc, endUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CanStaffRegisterShiftAsync(staffUserId, branchId, startUtc, endUtc);

        Assert.True(result);
    }

    [Fact]
    public async Task ExpireDueContracts_CallsRepoWithTodayVnDate()
    {
        _repoMock.Setup(r => r.ExpireDueContractsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var count = await _service.ExpireDueContractsAsync();

        Assert.Equal(3, count);
        _repoMock.Verify(r => r.ExpireDueContractsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
