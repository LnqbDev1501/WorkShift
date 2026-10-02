using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class StaffContractAcceptanceAndShiftRegistrationTests
{
    private readonly Mock<IEmploymentContractRepo> repo = new();
    private readonly Guid staffId = Guid.NewGuid();
    private readonly Guid managerId = Guid.NewGuid();

    private StaffEmploymentContractService Service() => new(repo.Object);

    [Fact]
    public async Task AcceptContract_BeforeStartDate_TransitionsToScheduled()
    {
        var futureStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(3));
        var contract = new EmploymentContract
        {
            ContractId = 1,
            UserId = staffId,
            ContractNumber = "EC-100",
            Status = "PENDING_STAFF",
            StartDate = futureStartDate,
            HourlyRate = 25000,
            EmployeePositionSnapshot = "Staff",
            BranchId = 1,
            RegionId = 1,
            CreatedBy = managerId
        };

        repo.Setup(r => r.GetStaffContractByIdAsync(1, staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        string? capturedStatus = null;
        repo.Setup(r => r.StaffAcceptContractAsync(1, staffId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<int, Guid, string, DateTime, CancellationToken>((_, _, status, _, _) => capturedStatus = status)
            .Returns(Task.CompletedTask);

        var result = await Service().AcceptContractAsync(staffId, 1);

        Assert.Equal("SCHEDULED", capturedStatus);
    }

    [Fact]
    public async Task AcceptContract_OnStartDate_TransitionsToActive()
    {
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var contract = new EmploymentContract
        {
            ContractId = 2,
            UserId = staffId,
            ContractNumber = "EC-101",
            Status = "PENDING_STAFF",
            StartDate = todayVn,
            HourlyRate = 25000,
            EmployeePositionSnapshot = "Staff",
            BranchId = 1,
            RegionId = 1,
            CreatedBy = managerId
        };

        repo.Setup(r => r.GetStaffContractByIdAsync(2, staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        string? capturedStatus = null;
        repo.Setup(r => r.StaffAcceptContractAsync(2, staffId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<int, Guid, string, DateTime, CancellationToken>((_, _, status, _, _) => capturedStatus = status)
            .Returns(Task.CompletedTask);

        var result = await Service().AcceptContractAsync(staffId, 2);

        Assert.Equal("ACTIVE", capturedStatus);
    }

    [Fact]
    public async Task AcceptContract_AfterStartDate_BlocksOperation()
    {
        var pastStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-2));
        var contract = new EmploymentContract
        {
            ContractId = 3,
            UserId = staffId,
            ContractNumber = "EC-102",
            Status = "PENDING_STAFF",
            StartDate = pastStartDate,
            HourlyRate = 25000,
            EmployeePositionSnapshot = "Staff",
            BranchId = 1,
            RegionId = 1,
            CreatedBy = managerId
        };

        repo.Setup(r => r.GetStaffContractByIdAsync(3, staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service().AcceptContractAsync(staffId, 3));
        Assert.Contains("quá ngày bắt đầu", ex.Message);

        repo.Verify(r => r.StaffAcceptContractAsync(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Worker_OnlyActivatesScheduledContractsDueToday()
    {
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        repo.Setup(r => r.ActivateDueScheduledContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var count = await Service().ActivateDueScheduledContractsAsync();

        Assert.Equal(5, count);
        repo.Verify(r => r.ActivateDueScheduledContractsAsync(todayVn, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateShiftRegistration_BlocksIfNoActiveContract()
    {
        var shiftStart = DateTime.UtcNow.AddDays(1);
        var shiftEnd = shiftStart.AddHours(8);

        repo.Setup(r => r.HasActiveContractForShiftRegistrationAsync(staffId, 1, shiftStart, shiftEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service().ValidateStaffContractForShiftRegistrationAsync(staffId, 1, shiftStart, shiftEnd));

        Assert.Contains("ACTIVE", ex.Message);
    }

    [Fact]
    public async Task ValidateShiftRegistration_SucceedsIfActiveContractExists()
    {
        var shiftStart = DateTime.UtcNow.AddDays(1);
        var shiftEnd = shiftStart.AddHours(8);

        repo.Setup(r => r.HasActiveContractForShiftRegistrationAsync(staffId, 1, shiftStart, shiftEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Service().ValidateStaffContractForShiftRegistrationAsync(staffId, 1, shiftStart, shiftEnd);

        repo.Verify(r => r.HasActiveContractForShiftRegistrationAsync(staffId, 1, shiftStart, shiftEnd, It.IsAny<CancellationToken>()), Times.Once);
    }
}
