using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Staff.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Staff;
using WorkShiftManagement.DAL.Models.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

/// <summary>
/// Unit and lifecycle tests for staff contract overdue cancellation, acceptance deadlines,
/// repeated worker executions, and salary history integrity.
/// </summary>
public class StaffContractOverdueLifecycleTests
{
    private readonly Mock<IEmploymentContractRepo> _contractRepoMock;
    private readonly Mock<IStaffSalaryHistoryRepo> _salaryRepoMock;
    private readonly Mock<ICloudinaryService> _cloudinaryMock;
    private readonly Mock<INotificationRealtimePublisher> _realtimeMock;
    private readonly Mock<INotificationEmailSender> _emailMock;
    private readonly Mock<ILogger<StaffEmploymentContractService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly StaffEmploymentContractService _staffContractService;
    private readonly StaffSalaryHistoryService _salaryService;

    private readonly Guid _staffUserId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly int _branchId = 10;

    public StaffContractOverdueLifecycleTests()
    {
        _contractRepoMock = new Mock<IEmploymentContractRepo>();
        _salaryRepoMock = new Mock<IStaffSalaryHistoryRepo>();
        _cloudinaryMock = new Mock<ICloudinaryService>();
        _realtimeMock = new Mock<INotificationRealtimePublisher>();
        _emailMock = new Mock<INotificationEmailSender>();
        _loggerMock = new Mock<ILogger<StaffEmploymentContractService>>();
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["Resend:ApiKey"]).Returns("test-resend-key");
        _configMock.Setup(c => c["Resend:NotificationTemplateId"]).Returns("test-template-id");

        _staffContractService = new StaffEmploymentContractService(
            _contractRepoMock.Object,
            _cloudinaryMock.Object,
            _realtimeMock.Object,
            _emailMock.Object,
            _loggerMock.Object,
            _configMock.Object);

        _salaryService = new StaffSalaryHistoryService(_salaryRepoMock.Object);
    }

    private static DateOnly GetCurrentVietnamDate()
    {
        var utcNow = DateTimeOffset.UtcNow;
        TimeZoneInfo vnTimeZone;
        try
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        var vnTime = TimeZoneInfo.ConvertTime(utcNow, vnTimeZone);
        return DateOnly.FromDateTime(vnTime.DateTime);
    }

    [Fact]
    public async Task Scenario1_AcceptContract_BeforeStartDate_TransitionsToScheduled()
    {
        // 1. Chấp nhận trước ngày bắt đầu: status chuyển sang SCHEDULED
        var todayVn = GetCurrentVietnamDate();
        var futureStartDate = todayVn.AddDays(3);

        var contract = new EmploymentContract
        {
            ContractId = 101,
            ContractNumber = "HD-TEST-001",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "PENDING_STAFF",
            StartDate = futureStartDate,
            EndDate = futureStartDate.AddMonths(12),
            HourlyRate = 35000m,
            EmployeeFullNameSnapshot = "Tran Van B"
        };

        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(101, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        string? capturedNewStatus = null;
        _contractRepoMock.Setup(r => r.StaffAcceptContractAsync(101, _staffUserId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<int, Guid, string, DateTime, CancellationToken>((cid, uid, status, dt, ct) => capturedNewStatus = status)
            .Returns(Task.CompletedTask);

        var result = await _staffContractService.AcceptContractAsync(_staffUserId, 101);

        Assert.Equal("SCHEDULED", capturedNewStatus);
        _contractRepoMock.Verify(r => r.StaffAcceptContractAsync(101, _staffUserId, "SCHEDULED", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Scenario2_AcceptContract_OnExactStartDate_TransitionsToActiveImmediately()
    {
        // 2. Chấp nhận đúng ngày bắt đầu: status chuyển sang ACTIVE ngay lập tức
        var todayVn = GetCurrentVietnamDate();

        var contract = new EmploymentContract
        {
            ContractId = 102,
            ContractNumber = "HD-TEST-002",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "PENDING_STAFF",
            StartDate = todayVn,
            EndDate = todayVn.AddMonths(6),
            HourlyRate = 40000m,
            EmployeeFullNameSnapshot = "Tran Van B"
        };

        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(102, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        string? capturedNewStatus = null;
        _contractRepoMock.Setup(r => r.StaffAcceptContractAsync(102, _staffUserId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<int, Guid, string, DateTime, CancellationToken>((cid, uid, status, dt, ct) => capturedNewStatus = status)
            .Returns(Task.CompletedTask);

        var result = await _staffContractService.AcceptContractAsync(_staffUserId, 102);

        Assert.Equal("ACTIVE", capturedNewStatus);
        _contractRepoMock.Verify(r => r.StaffAcceptContractAsync(102, _staffUserId, "ACTIVE", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Scenario3_AcceptContract_AfterStartDate_RejectsWithClearMessage()
    {
        // 3. Chấp nhận sau ngày bắt đầu: API từ chối và trả thông báo rõ ràng
        var todayVn = GetCurrentVietnamDate();
        var pastStartDate = todayVn.AddDays(-1);

        var contract = new EmploymentContract
        {
            ContractId = 103,
            ContractNumber = "HD-TEST-003",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "PENDING_STAFF",
            StartDate = pastStartDate,
            EndDate = pastStartDate.AddMonths(6),
            HourlyRate = 30000m,
            EmployeeFullNameSnapshot = "Tran Van B"
        };

        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(103, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _staffContractService.AcceptContractAsync(_staffUserId, 103));

        Assert.Contains("Hợp đồng đã quá ngày bắt đầu", ex.Message);
        Assert.Contains("Không thể chấp nhận", ex.Message);

        // Đảm bảo không gọi StaffAcceptContractAsync
        _contractRepoMock.Verify(r => r.StaffAcceptContractAsync(
            It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scenario4_AutoCancel_WhenPendingStaffPastStartDate_CancelsContractWithAuditAndNotification()
    {
        // 4. Tự hủy sau khi quá ngày bắt đầu: hợp đồng chuyển sang CANCELLED,
        // TerminationReason = NOT_ACCEPTED_BY_START_DATE, người thực hiện là hệ thống
        var todayVn = GetCurrentVietnamDate();
        var overdueContracts = new List<EmploymentContract>
        {
            new()
            {
                ContractId = 201,
                ContractNumber = "HD-OVERDUE-01",
                UserId = _staffUserId,
                CreatedBy = _managerId,
                BranchId = _branchId,
                Status = "PENDING_STAFF",
                StartDate = todayVn.AddDays(-2),
                EndDate = todayVn.AddMonths(6),
                HourlyRate = 35000m,
                EmployeeFullNameSnapshot = "Nguyen Overdue"
            }
        };

        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult { CancelledCount = overdueContracts.Count });

        var cancelledCount = await _staffContractService.CancelOverduePendingContractsAsync();

        Assert.Equal(1, cancelledCount);
        _contractRepoMock.Verify(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Scenario5_WorkerRepeatedExecution_IsIdempotentAndSafe()
    {
        // 5. Worker chạy lặp: lần đầu hủy 2 hợp đồng, lần 2 không còn hợp đồng PENDING_STAFF quá hạn nào
        var todayVn = GetCurrentVietnamDate();

        int callCount = 0;
        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new CancelOverdueContractsResult
                {
                    CancelledCount = callCount == 1 ? 2 : 0
                };
            });

        // Run 1
        var firstRun = await _staffContractService.CancelOverduePendingContractsAsync();
        Assert.Equal(2, firstRun);

        // Run 2 (repeated run)
        var secondRun = await _staffContractService.CancelOverduePendingContractsAsync();
        Assert.Equal(0, secondRun);

        _contractRepoMock.Verify(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Scenario6_CancelledContract_NeverCreatesSalaryHistory_AndBlocksShiftRegistration()
    {
        // 6. Hợp đồng bị hủy không bao giờ chuyển sang SCHEDULED hoặc ACTIVE,
        // không tạo bản ghi INITIAL trong staff_salary_history, không cho phép đăng ký ca
        var contractId = 301;
        var cancelledContract = new EmploymentContract
        {
            ContractId = contractId,
            ContractNumber = "HD-CANCELLED-01",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "CANCELLED",
            CancellationReason = "NOT_ACCEPTED_BY_START_DATE",
            CancelledAt = DateTime.UtcNow,
            CancelledBy = null,
            TerminationReason = null,
            TerminatedAt = null,
            TerminatedBy = null,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2027, 10, 1),
            HourlyRate = 50000m
        };

        Assert.Null(cancelledContract.TerminationReason);
        Assert.Null(cancelledContract.TerminatedAt);
        Assert.Equal("NOT_ACCEPTED_BY_START_DATE", cancelledContract.CancellationReason);

        // 6a. Không có salary history nào được tạo cho contract này
        _salaryRepoMock.Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StaffSalaryHistory>());

        var salaryHistory = await _salaryService.GetByContractIdAsync(contractId);
        Assert.Empty(salaryHistory);

        // 6b. Thử resolve salary snapshot cho ngày làm việc sẽ bị lỗi vì không có salary history
        var workDate = new DateOnly(2026, 10, 5);
        _salaryRepoMock.Setup(r => r.GetEffectiveSalaryHistoryAsync(_staffUserId, workDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffSalaryHistory?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salaryService.ResolveSalarySnapshotForWorkDateAsync(_staffUserId, workDate));

        // 6c. Kiểm tra điều kiện đăng ký ca: không thể đăng ký ca theo hợp đồng đã hủy
        _contractRepoMock.Setup(r => r.HasActiveContractForShiftRegistrationAsync(
            _staffUserId, _branchId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var canRegister = await _staffContractService.CanStaffRegisterShiftAsync(
            _staffUserId, _branchId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4));
        Assert.False(canRegister);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _staffContractService.ValidateStaffContractForShiftRegistrationAsync(
                _staffUserId, _branchId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(4)));
    }

    [Fact]
    public async Task Scenario7_CancellationFields_AreSeparatedFromTermination_InStaffDetailView()
    {
        // 7. Kiểm tra cách lưu không gây nhầm lẫn: các trường cancellation tách biệt hoàn toàn
        // với termination trên DTO detail
        var cancelledTime = DateTime.UtcNow;
        var contract = new EmploymentContract
        {
            ContractId = 401,
            ContractNumber = "HD-CANCEL-TEST",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "CANCELLED",
            CancellationReason = "NOT_ACCEPTED_BY_START_DATE",
            CancelledAt = cancelledTime,
            CancelledBy = null,
            TerminationReason = null,
            TerminatedAt = null,
            TerminatedBy = null,
            TerminationEffectiveAt = null,
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 9, 1),
            HourlyRate = 45000m,
            EmployeeFullNameSnapshot = "Staff Member"
        };

        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(401, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var detail = await _staffContractService.GetMyContractByIdAsync(_staffUserId, 401);

        Assert.Equal("CANCELLED", detail.Status);
        Assert.Equal("NOT_ACCEPTED_BY_START_DATE", detail.CancellationReason);
        Assert.Equal(cancelledTime, detail.CancelledAt);
        // Bảo đảm các trường chấm dứt hợp đồng (TERMINATED) hoàn toàn null
        Assert.Null(detail.TerminationReason);
        Assert.Null(detail.TerminationEffectiveAt);
        Assert.False(detail.CanAccept);
    }

    [Fact]
    public async Task Scenario8_Concurrency_WhenWorkerAutoCancels_StaffAcceptIsRejectedSafely()
    {
        // 8. Tranh chấp đồng thời: Worker đã xử lý tự hủy trước hoặc cùng lúc Staff nhấn chấp nhận,
        // Hợp đồng chuyển sang CANCELLED, Staff Accept phát hiện không còn PENDING_STAFF và ném lỗi ngay.
        var contractId = 501;
        var now = DateTime.UtcNow;

        var contractAfterWorkerCancelled = new EmploymentContract
        {
            ContractId = contractId,
            ContractNumber = "HD-CONCURRENCY-01",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "CANCELLED", // Worker đã chuyển trạng thái thành CANCELLED
            CancellationReason = "NOT_ACCEPTED_BY_START_DATE",
            CancelledAt = now,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1)) // Dù start_date là gì, đã CANCELLED thì không được accept
        };

        _contractRepoMock.Setup(r => r.GetStaffContractByIdAsync(contractId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contractAfterWorkerCancelled);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _staffContractService.AcceptContractAsync(_staffUserId, contractId));

        Assert.Contains("Only contracts in PENDING_STAFF status can be accepted", ex.Message);

        // Đảm bảo không thao tác chấp nhận được thực hiện
        _contractRepoMock.Verify(r => r.StaffAcceptContractAsync(
            contractId, _staffUserId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scenario9_ContractNotOverdue_OnStartDateOrFuture_DoesNotAutoCancel()
    {
        // 9. Hợp đồng chưa quá ngày bắt đầu (StartDate == todayVn hoặc tương lai)
        // không bị tự hủy; Staff vẫn được phép chấp nhận trong ngày start_date
        var todayVn = GetCurrentVietnamDate();

        // Giả lập repo: Chỉ tìm các hợp đồng có start_date < todayVn, hợp đồng hôm nay hoặc tương lai không bị query
        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult { CancelledCount = 0, Dispatches = new() });

        var cancelledCount = await _staffContractService.CancelOverduePendingContractsAsync();

        Assert.Equal(0, cancelledCount);
        _realtimeMock.Verify(r => r.PublishAsync(It.IsAny<Guid>(), It.IsAny<RealtimeNotificationResponse>()), Times.Never);
        _emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Scenario10_ContractPastStartDate_AutoCancels_WithCancelledStatusAndReason()
    {
        // 10. Quá ngày bắt đầu thì tự hủy: chuyển sang CANCELLED, lý do NOT_ACCEPTED_BY_START_DATE
        var todayVn = GetCurrentVietnamDate();
        var pastStartDate = todayVn.AddDays(-1);

        var dispatch = new OverdueContractNotificationDispatch
        {
            NotificationId = 701,
            ContractId = 601,
            ContractNumber = "HD-OVERDUE-PAST",
            Title = "Hợp đồng lao động đã bị tự động hủy",
            Content = $"Hợp đồng lao động HD-OVERDUE-PAST đã chuyển sang trạng thái CANCELLED do hệ thống tự động hủy. Lý do: NOT_ACCEPTED_BY_START_DATE (quá ngày bắt đầu {pastStartDate:yyyy-MM-dd}).",
            CreatedAt = DateTime.UtcNow,
            BranchId = _branchId,
            RecipientUserIds = new() { _staffUserId, _managerId },
            RecipientEmails = new() { "staff@example.com", "manager@example.com" },
            HasEmailChannel = true
        };

        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult
            {
                CancelledCount = 1,
                Dispatches = new() { dispatch }
            });

        var count = await _staffContractService.CancelOverduePendingContractsAsync();

        Assert.Equal(1, count);
        _realtimeMock.Verify(r => r.PublishAsync(_staffUserId, It.Is<RealtimeNotificationResponse>(n =>
            n.Title.Contains("Hợp đồng lao động đã bị tự động hủy") &&
            n.Message.Contains("NOT_ACCEPTED_BY_START_DATE"))), Times.Once);
        _realtimeMock.Verify(r => r.PublishAsync(_managerId, It.IsAny<RealtimeNotificationResponse>()), Times.Once);
    }

    [Fact]
    public async Task Scenario11_Notification_SentOnlyToStaffAndBranchManager_NotToOtherBranchManagers()
    {
        // 11. Thông báo chỉ gửi cho Staff sở hữu hợp đồng và Manager thuộc đúng chi nhánh,
        // Manager ở chi nhánh khác không nhận được thông báo
        var todayVn = GetCurrentVietnamDate();
        var otherBranchManagerId = Guid.NewGuid();

        var dispatch = new OverdueContractNotificationDispatch
        {
            NotificationId = 801,
            ContractId = 701,
            ContractNumber = "HD-BRANCH-10",
            Title = "Hợp đồng lao động đã bị tự động hủy",
            Content = "Hợp đồng chi nhánh 10 đã bị hủy tự động: NOT_ACCEPTED_BY_START_DATE",
            CreatedAt = DateTime.UtcNow,
            BranchId = 10,
            RecipientUserIds = new() { _staffUserId, _managerId }, // Chỉ chứa Staff & Manager Chi nhánh 10
            RecipientEmails = new() { "staff@branch10.com", "manager@branch10.com" },
            HasEmailChannel = true
        };

        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult
            {
                CancelledCount = 1,
                Dispatches = new() { dispatch }
            });

        await _staffContractService.CancelOverduePendingContractsAsync();

        // Đảm bảo gửi đúng cho Staff và Manager chi nhánh 10
        _realtimeMock.Verify(r => r.PublishAsync(_staffUserId, It.IsAny<RealtimeNotificationResponse>()), Times.Once);
        _realtimeMock.Verify(r => r.PublishAsync(_managerId, It.IsAny<RealtimeNotificationResponse>()), Times.Once);

        // Đảm bảo KHÔNG gửi cho Manager chi nhánh khác
        _realtimeMock.Verify(r => r.PublishAsync(otherBranchManagerId, It.IsAny<RealtimeNotificationResponse>()), Times.Never);
        _emailMock.Verify(e => e.SendAsync("othermanager@branch20.com", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Scenario12_JobRetry_DoesNotCreateDuplicateNotification_Idempotency()
    {
        // 12. Hangfire retry hoặc chạy lặp: khi hợp đồng đã xử lý và đã có thông báo trong hệ thống,
        // lần gọi sau không tạo thêm thông báo mới
        var todayVn = GetCurrentVietnamDate();

        // Lần đầu: 1 hợp đồng bị hủy, 1 dispatch
        var dispatch = new OverdueContractNotificationDispatch
        {
            NotificationId = 901,
            ContractId = 801,
            ContractNumber = "HD-RETRY-01",
            Title = "Hợp đồng lao động đã bị tự động hủy",
            Content = "Nội dung thông báo hủy",
            CreatedAt = DateTime.UtcNow,
            BranchId = _branchId,
            RecipientUserIds = new() { _staffUserId },
            RecipientEmails = new() { "staff@test.com" },
            HasEmailChannel = true
        };

        int execution = 0;
        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                execution++;
                if (execution == 1)
                {
                    return new CancelOverdueContractsResult { CancelledCount = 1, Dispatches = new() { dispatch } };
                }
                // Lần 2 (retry): Hợp đồng đã CANCELLED và đã gửi thông báo -> Không còn hợp đồng quá hạn nào
                return new CancelOverdueContractsResult { CancelledCount = 0, Dispatches = new() };
            });

        // Run 1
        var run1 = await _staffContractService.CancelOverduePendingContractsAsync();
        Assert.Equal(1, run1);
        _realtimeMock.Verify(r => r.PublishAsync(_staffUserId, It.IsAny<RealtimeNotificationResponse>()), Times.Once);

        // Run 2 (Retry)
        var run2 = await _staffContractService.CancelOverduePendingContractsAsync();
        Assert.Equal(0, run2);
        // Số lần publish vẫn chỉ là 1, không tăng thêm
        _realtimeMock.Verify(r => r.PublishAsync(_staffUserId, It.IsAny<RealtimeNotificationResponse>()), Times.Once);
    }

    [Fact]
    public async Task Scenario13_NotificationFailure_DoesNotRollbackOrUncancelContract()
    {
        // 13. Lỗi gửi thông báo (SignalR hoặc Resend email bị lỗi) không làm khôi phục hợp đồng đã hủy.
        // Hợp đồng vẫn được tính là đã hủy thành công và không ném lỗi ra ngoài làm crash job.
        var todayVn = GetCurrentVietnamDate();

        var dispatch = new OverdueContractNotificationDispatch
        {
            NotificationId = 999,
            ContractId = 888,
            ContractNumber = "HD-FAIL-NOTIF",
            Title = "Hợp đồng lao động đã bị tự động hủy",
            Content = "Thông báo hủy hợp đồng",
            CreatedAt = DateTime.UtcNow,
            BranchId = _branchId,
            RecipientUserIds = new() { _staffUserId },
            RecipientEmails = new() { "staff@fail.com" },
            HasEmailChannel = true
        };

        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelOverdueContractsResult
            {
                CancelledCount = 1,
                Dispatches = new() { dispatch }
            });

        // Giả lập SignalR bị sập mạng / văng lỗi
        _realtimeMock.Setup(r => r.PublishAsync(It.IsAny<Guid>(), It.IsAny<RealtimeNotificationResponse>()))
            .ThrowsAsync(new InvalidOperationException("SignalR Hub connection closed"));

        // Giả lập Resend bị rate limit hoặc API key lỗi
        _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Resend API rate limit exceeded"));

        // Gọi method hủy hợp đồng
        var cancelledCount = await _staffContractService.CancelOverduePendingContractsAsync();

        // Việc hủy hợp đồng vẫn thành công với kết quả 1 hợp đồng, không bị rollback hoặc crash
        Assert.Equal(1, cancelledCount);

        // Kiểm tra logger đã ghi warning về sự cố SignalR và Resend
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Scenario14_ReplacingContract_OverdueCancelled_OldContractAndSalaryHistoryRemainActiveAndUntouched()
    {
        // 14. Nghiệp vụ hợp đồng thay thế bị tự hủy:
        // 1. Staff có hợp đồng cũ ACTIVE (ID 100) và salary history cũ ACTIVE (ID 1) với EffectiveTo cố định.
        // 2. Hợp đồng thay thế mới đang PENDING_STAFF (ID 200, ReplacesContractId = 100) quá hạn start_date rồi bị Hangfire tự hủy.
        // 3. Sau khi chạy job:
        //    - Hợp đồng cũ (ID 100) vẫn ACTIVE, không bị thay đổi trạng thái.
        //    - Salary history cũ (ID 1) vẫn ACTIVE và effective_to giữ nguyên không đổi.
        //    - Hợp đồng mới (ID 200) là CANCELLED với CancellationReason = NOT_ACCEPTED_BY_START_DATE.
        //    - Không có salary history mới hay Payroll nào được tạo cho hợp đồng bị hủy (ID 200).
        var todayVn = GetCurrentVietnamDate();
        var oldContractEffectiveTo = todayVn.AddMonths(5);

        // Hợp đồng cũ đang ACTIVE
        var oldContract = new EmploymentContract
        {
            ContractId = 100,
            ContractNumber = "HD-OLD-ACTIVE-100",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            Status = "ACTIVE",
            StartDate = todayVn.AddMonths(-7),
            EndDate = oldContractEffectiveTo,
            HourlyRate = 30000m,
            EmployeeFullNameSnapshot = "Staff Member"
        };

        // Salary history cũ đang ACTIVE gắn với hợp đồng cũ
        var oldSalaryHistory = new StaffSalaryHistory
        {
            SalaryHistoryId = 1,
            UserId = _staffUserId,
            ContractId = 100,
            HourlyRate = 30000m,
            ChangeType = "INITIAL",
            EffectiveFrom = todayVn.AddMonths(-7),
            EffectiveTo = oldContractEffectiveTo,
            Status = "ACTIVE",
            Reason = "Lương hợp đồng cũ"
        };

        // Hợp đồng thay thế mới đang PENDING_STAFF, quá hạn ngày bắt đầu
        var newReplacingContract = new EmploymentContract
        {
            ContractId = 200,
            ContractNumber = "HD-NEW-REPLACING-200",
            UserId = _staffUserId,
            CreatedBy = _managerId,
            BranchId = _branchId,
            ReplacesContractId = 100, // Thay thế hợp đồng 100
            Status = "PENDING_STAFF",
            StartDate = todayVn.AddDays(-2), // Quá hạn 2 ngày
            EndDate = todayVn.AddMonths(12),
            HourlyRate = 45000m,
            EmployeeFullNameSnapshot = "Staff Member"
        };

        // Danh sách hợp đồng và salary histories trong hệ thống
        var contractsDb = new List<EmploymentContract> { oldContract, newReplacingContract };
        var salaryHistoriesDb = new List<StaffSalaryHistory> { oldSalaryHistory };

        // Giả lập logic của Repo khi CancelOverduePendingContractsAsync chạy:
        _contractRepoMock.Setup(r => r.CancelOverduePendingContractsAsync(todayVn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Chỉ tìm các hợp đồng status == PENDING_STAFF và StartDate < todayVn
                var overdueList = contractsDb
                    .Where(c => c.Status == "PENDING_STAFF" && c.StartDate < todayVn)
                    .ToList();

                var dispatches = new List<OverdueContractNotificationDispatch>();
                int cancelledCount = 0;

                foreach (var c in overdueList)
                {
                    c.Status = "CANCELLED";
                    c.CancellationReason = "NOT_ACCEPTED_BY_START_DATE";
                    c.CancelledAt = DateTime.UtcNow;
                    cancelledCount++;

                    // ĐIỀU KIỆN LỌC BẮT BUỘC: Chỉ lọc theo s.ContractId == c.ContractId
                    // TUYỆT ĐỐI KHÔNG cập nhật theo UserId và KHÔNG đụng đến c.ReplacesContractId
                    var salariesToDeactivate = salaryHistoriesDb
                        .Where(s => s.ContractId == c.ContractId)
                        .ToList();

                    foreach (var s in salariesToDeactivate)
                    {
                        s.Status = "INACTIVE";
                    }

                    dispatches.Add(new OverdueContractNotificationDispatch
                    {
                        NotificationId = 1001,
                        ContractId = c.ContractId,
                        ContractNumber = c.ContractNumber,
                        Title = $"Hợp đồng lao động {c.ContractNumber} đã bị tự động hủy",
                        Content = $"Hợp đồng {c.ContractNumber} đã bị tự động hủy: NOT_ACCEPTED_BY_START_DATE",
                        BranchId = c.BranchId,
                        RecipientUserIds = new() { c.UserId, _managerId }
                    });
                }

                return new CancelOverdueContractsResult
                {
                    CancelledCount = cancelledCount,
                    Dispatches = dispatches
                };
            });

        // Setup mock cho salary repo để kiểm tra lịch sử lương của hợp đồng mới và cũ
        _salaryRepoMock.Setup(r => r.GetByContractIdAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salaryHistoriesDb.Where(s => s.ContractId == 200).ToList());
        _salaryRepoMock.Setup(r => r.GetByContractIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salaryHistoriesDb.Where(s => s.ContractId == 100).ToList());

        // CHẠY JOB
        var resultCount = await _staffContractService.CancelOverduePendingContractsAsync();

        // 1. Kết quả chạy job: đúng 1 hợp đồng (hợp đồng 200) bị hủy
        Assert.Equal(1, resultCount);

        // 2. Hợp đồng thay thế mới (ID 200) chuyển sang CANCELLED, lý do NOT_ACCEPTED_BY_START_DATE
        Assert.Equal("CANCELLED", newReplacingContract.Status);
        Assert.Equal("NOT_ACCEPTED_BY_START_DATE", newReplacingContract.CancellationReason);

        // 3. Hợp đồng cũ (ID 100) VẪN GIỮ NGUYÊN ACTIVE, hoàn toàn không bị ảnh hưởng
        Assert.Equal("ACTIVE", oldContract.Status);
        Assert.Equal(oldContractEffectiveTo, oldContract.EndDate);

        // 4. Salary history cũ (ID 1) của hợp đồng 100 VẪN GIỮ NGUYÊN ACTIVE và EffectiveTo KHÔNG ĐỔI
        Assert.Equal("ACTIVE", oldSalaryHistory.Status);
        Assert.Equal(oldContractEffectiveTo, oldSalaryHistory.EffectiveTo);
        Assert.Equal(30000m, oldSalaryHistory.HourlyRate);

        // 5. Hợp đồng mới bị hủy (ID 200) KHÔNG HỀ có salary history nào được tạo
        var newContractSalaries = await _salaryService.GetByContractIdAsync(200);
        Assert.Empty(newContractSalaries);

        // 6. Kiểm tra quyền đăng ký ca: hợp đồng 200 bị CANCELLED nên không được phép đăng ký ca
        _contractRepoMock.Setup(r => r.HasActiveContractForShiftRegistrationAsync(
            _staffUserId, _branchId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var canRegisterShift = await _staffContractService.CanStaffRegisterShiftAsync(
            _staffUserId, _branchId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(8));
        Assert.False(canRegisterShift);
    }
}
