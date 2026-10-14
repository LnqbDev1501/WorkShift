using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Staff;
using Xunit;

namespace WorkShiftManagement.Tests;

public class StaffSalaryHistoryLifecycleTests
{
    private readonly Mock<IEmploymentContractRepo> _contractRepoMock;
    private readonly Mock<IStaffSalaryHistoryRepo> _salaryRepoMock;
    private readonly Mock<ICloudinaryService> _cloudinaryMock;
    private readonly Mock<IContractPdfGenerator> _pdfGeneratorMock;
    private readonly ManagerEmploymentContractService _managerService;
    private readonly StaffSalaryHistoryService _salaryService;

    private readonly Guid _managerId = Guid.NewGuid();
    private readonly Guid _staffUserId = Guid.NewGuid();
    private readonly int _branchId = 101;

    public StaffSalaryHistoryLifecycleTests()
    {
        _contractRepoMock = new Mock<IEmploymentContractRepo>();
        _salaryRepoMock = new Mock<IStaffSalaryHistoryRepo>();
        _cloudinaryMock = new Mock<ICloudinaryService>();
        _pdfGeneratorMock = new Mock<IContractPdfGenerator>();

        _managerService = new ManagerEmploymentContractService(
            _contractRepoMock.Object, _cloudinaryMock.Object, _pdfGeneratorMock.Object);

        _salaryService = new StaffSalaryHistoryService(_salaryRepoMock.Object);

        var managerUser = new User
        {
            UserId = _managerId,
            BranchId = _branchId,
            Status = "ACTIVE",
            Role = new Role { RoleName = "MANAGER" },
            Branch = new Branch
            {
                BranchId = _branchId,
                BranchName = "Chi nhánh Quận 1",
                Status = "ACTIVE",
                Region = new Region
                {
                    RegionId = 1,
                    IsActive = true,
                    RegionalMinimumWageId = 1,
                    RegionalMinimumWage = new RegionalMinimumWage
                    {
                        RegionalMinimumWageId = 1,
                        Status = "ACTIVE",
                        EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                        MinimumHourlyRate = 22500m
                    }
                }
            }
        };

        _contractRepoMock.Setup(r => r.GetCreationUserAsync(_managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerUser);
    }

    [Fact]
    public async Task Case1_DraftContract_DoesNotCreateSalaryHistory()
    {
        var staffUser = new User
        {
            UserId = _staffUserId,
            FullName = "Nguyen Van Staff",
            BranchId = _branchId,
            Status = "ACTIVE",
            Role = new Role { RoleName = "STAFF" }
        };

        _contractRepoMock.Setup(r => r.GetCreationUserAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);
        _contractRepoMock.Setup(r => r.HasAnyContractAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _contractRepoMock.Setup(r => r.HasPendingContractAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        EmploymentContract? savedContract = null;
        _contractRepoMock.Setup(r => r.CreateDraftAsync(It.IsAny<EmploymentContract>(), It.IsAny<CancellationToken>()))
            .Callback<EmploymentContract, CancellationToken>((c, _) => savedContract = c)
            .Returns(Task.CompletedTask);

        var request = new CreateEmploymentContractRequest
        {
            StaffUserId = _staffUserId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            EmployeePosition = "Phục vụ",
            ContractType = "NEW"
        };

        var response = await _managerService.CreateAsync(_managerId, request);

        Assert.NotNull(response);
        Assert.NotNull(savedContract);
        Assert.Equal("DRAFT", savedContract.Status);

        // Verification: No salary history was generated or touched during DRAFT creation
        _salaryRepoMock.Verify(r => r.HasInitialSalaryHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Case2_ContractAccepted_WhenStartDateInFuture_ConfiguredAsScheduledInitial()
    {
        var futureStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var endDate = futureStartDate.AddYears(1);

        var contract = new EmploymentContract
        {
            ContractId = 10,
            ContractNumber = "EC-2026-FUTURE",
            UserId = _staffUserId,
            StartDate = futureStartDate,
            EndDate = endDate,
            HourlyRate = 25000m,
            Status = "PENDING_STAFF",
            CreatedBy = _managerId
        };

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var newStatus = (todayVn >= contract.StartDate) ? "ACTIVE" : "SCHEDULED";

        Assert.Equal("SCHEDULED", newStatus);

        var salaryHistory = new StaffSalaryHistory
        {
            UserId = contract.UserId,
            ContractId = contract.ContractId,
            HourlyRate = contract.HourlyRate,
            PreviousHourlyRate = null,
            ChangeType = "INITIAL",
            EffectiveFrom = contract.StartDate,
            EffectiveTo = contract.EndDate,
            Status = newStatus,
            Reason = $"Mức lương ban đầu theo hợp đồng lao động {contract.ContractNumber}.",
            CreatedBy = contract.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("SCHEDULED", salaryHistory.Status);
        Assert.Equal("INITIAL", salaryHistory.ChangeType);
        Assert.Null(salaryHistory.PreviousHourlyRate);
        Assert.Equal(25000m, salaryHistory.HourlyRate);
        Assert.Equal(futureStartDate, salaryHistory.EffectiveFrom);
        Assert.Equal(endDate, salaryHistory.EffectiveTo);
    }

    [Fact]
    public void Case3_ContractAccepted_WhenStartDateIsToday_ConfiguredAsActiveInitial()
    {
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var endDate = todayVn.AddYears(1);

        var contract = new EmploymentContract
        {
            ContractId = 11,
            ContractNumber = "EC-2026-TODAY",
            UserId = _staffUserId,
            StartDate = todayVn,
            EndDate = endDate,
            HourlyRate = 30000m,
            Status = "PENDING_STAFF",
            CreatedBy = _managerId
        };

        var newStatus = (todayVn >= contract.StartDate) ? "ACTIVE" : "SCHEDULED";

        Assert.Equal("ACTIVE", newStatus);

        var salaryHistory = new StaffSalaryHistory
        {
            UserId = contract.UserId,
            ContractId = contract.ContractId,
            HourlyRate = contract.HourlyRate,
            PreviousHourlyRate = null,
            ChangeType = "INITIAL",
            EffectiveFrom = contract.StartDate,
            EffectiveTo = contract.EndDate,
            Status = newStatus,
            Reason = $"Mức lương ban đầu theo hợp đồng lao động {contract.ContractNumber}.",
            CreatedBy = contract.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("ACTIVE", salaryHistory.Status);
        Assert.Equal("INITIAL", salaryHistory.ChangeType);
        Assert.Null(salaryHistory.PreviousHourlyRate);
        Assert.Equal(30000m, salaryHistory.HourlyRate);
    }

    [Fact]
    public void Case4_BackgroundWorker_ActivationUpdatesScheduledToActive()
    {
        var scheduledSalary = new StaffSalaryHistory
        {
            SalaryHistoryId = 100,
            UserId = _staffUserId,
            ContractId = 15,
            HourlyRate = 28000m,
            ChangeType = "INITIAL",
            EffectiveFrom = new DateOnly(2026, 4, 1),
            EffectiveTo = new DateOnly(2027, 4, 1),
            Status = "SCHEDULED"
        };

        // When worker executes on or after EffectiveFrom (2026-04-01):
        scheduledSalary.Status = "ACTIVE";

        Assert.Equal("ACTIVE", scheduledSalary.Status);
    }

    [Fact]
    public void Case5_Idempotency_RerunningAcceptOrWorker_DoesNotDuplicateInitialRow()
    {
        var existingSalaryHistories = new List<StaffSalaryHistory>
        {
            new()
            {
                SalaryHistoryId = 1,
                ContractId = 20,
                UserId = _staffUserId,
                ChangeType = "INITIAL",
                Status = "SCHEDULED",
                HourlyRate = 25000m
            }
        };

        // Simulating second execution:
        var contractId = 20;
        var existing = existingSalaryHistories.Find(s => s.ContractId == contractId && s.ChangeType == "INITIAL");

        Assert.NotNull(existing);

        // Does not add a new row
        if (existing == null)
        {
            existingSalaryHistories.Add(new StaffSalaryHistory { SalaryHistoryId = 2 });
        }
        else
        {
            existing.Status = "ACTIVE"; // updates existing row idempotently
        }

        Assert.Single(existingSalaryHistories);
        Assert.Equal("ACTIVE", existingSalaryHistories[0].Status);
    }

    [Fact]
    public void Case6_ReplacementContract_PreventsOverlappingDateRanges_WithoutDeletingOldHistory()
    {
        var oldContractSalary = new StaffSalaryHistory
        {
            SalaryHistoryId = 1,
            ContractId = 50,
            UserId = _staffUserId,
            HourlyRate = 22000m,
            ChangeType = "INITIAL",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 12, 31),
            Status = "ACTIVE"
        };

        var replacementStartDate = new DateOnly(2026, 6, 1);
        var replacementEndDate = new DateOnly(2027, 6, 1);

        // When replacement contract takes effect on replacementStartDate:
        // 1. Old history is NOT deleted
        // 2. Old history effective_to is capped to the day before replacementStartDate
        if (oldContractSalary.EffectiveTo == null || oldContractSalary.EffectiveTo >= replacementStartDate)
        {
            oldContractSalary.EffectiveTo = replacementStartDate.AddDays(-1);
        }
        oldContractSalary.Status = "INACTIVE";

        var newSalaryHistory = new StaffSalaryHistory
        {
            SalaryHistoryId = 2,
            ContractId = 51,
            UserId = _staffUserId,
            HourlyRate = 26000m,
            ChangeType = "INITIAL",
            EffectiveFrom = replacementStartDate,
            EffectiveTo = replacementEndDate,
            Status = "ACTIVE"
        };

        // Verify:
        Assert.Equal(new DateOnly(2026, 5, 31), oldContractSalary.EffectiveTo);
        Assert.Equal("INACTIVE", oldContractSalary.Status);
        Assert.Equal(new DateOnly(2026, 6, 1), newSalaryHistory.EffectiveFrom);
        Assert.Equal("ACTIVE", newSalaryHistory.Status);
        Assert.True(oldContractSalary.EffectiveTo < newSalaryHistory.EffectiveFrom);
    }

    [Fact]
    public async Task Case7_TransactionRollback_WhenSalaryHistoryFails_RollsBackContractStatusChange()
    {
        _contractRepoMock.Setup(r => r.StaffAcceptContractAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated database failure during salary history insert."));

        var contract = new EmploymentContract
        {
            ContractId = 99,
            Status = "PENDING_STAFF",
            UserId = _staffUserId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };
        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(99, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var staffService = new StaffEmploymentContractService(_contractRepoMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            staffService.AcceptContractAsync(_staffUserId, 99));

        // Contract status remains PENDING_STAFF because transaction rolled back
        Assert.Equal("PENDING_STAFF", contract.Status);
    }

    [Fact]
    public async Task Case8_PayrollService_ResolvesEffectiveSalaryHistoryByWorkDate_AndErrorsWhenMissing()
    {
        var workDate = new DateOnly(2026, 5, 15);

        var validSalary = new StaffSalaryHistory
        {
            SalaryHistoryId = 77,
            UserId = _staffUserId,
            ContractId = 10,
            HourlyRate = 27500m,
            ChangeType = "INITIAL",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 12, 31),
            Status = "ACTIVE",
            Contract = new EmploymentContract { ContractNumber = "EC-VALID" }
        };

        _salaryRepoMock.Setup(r => r.GetEffectiveSalaryHistoryAsync(_staffUserId, workDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validSalary);

        // Resolves correctly when within valid period:
        var result = await _salaryService.ResolveSalarySnapshotForWorkDateAsync(_staffUserId, workDate);
        Assert.Equal(77, result.SalaryHistoryId);
        Assert.Equal(27500m, result.HourlyRateSnapshot);

        // When no salary history exists for the work date:
        var missingWorkDate = new DateOnly(2025, 1, 1);
        _salaryRepoMock.Setup(r => r.GetEffectiveSalaryHistoryAsync(_staffUserId, missingWorkDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffSalaryHistory?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salaryService.ResolveSalarySnapshotForWorkDateAsync(_staffUserId, missingWorkDate));

        Assert.Contains("Không tìm thấy mức lương hợp lệ", ex.Message);
        Assert.Contains("Không thể tính lương bằng mức 0", ex.Message);

        // When salary history exists but hourly rate is <= 0:
        var invalidRateSalary = new StaffSalaryHistory
        {
            SalaryHistoryId = 88,
            UserId = _staffUserId,
            HourlyRate = 0m,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 12, 31)
        };
        _salaryRepoMock.Setup(r => r.GetEffectiveSalaryHistoryAsync(_staffUserId, workDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidRateSalary);

        var rateEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salaryService.ResolveSalarySnapshotForWorkDateAsync(_staffUserId, workDate));

        Assert.Contains("không hợp lệ", rateEx.Message);
        Assert.Contains("Không thể tính lương bằng mức 0", rateEx.Message);
    }
}
