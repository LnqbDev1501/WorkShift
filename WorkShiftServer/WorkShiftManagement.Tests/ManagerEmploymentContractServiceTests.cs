using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;

public class ManagerEmploymentContractServiceTests
{
    private readonly Mock<IEmploymentContractRepo> _repoMock;
    private readonly Mock<ICloudinaryService> _cloudinaryMock;
    private readonly Mock<IContractPdfGenerator> _pdfGeneratorMock;
    private readonly ManagerEmploymentContractService _service;
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly int _branchId = 10;

    public ManagerEmploymentContractServiceTests()
    {
        _repoMock = new Mock<IEmploymentContractRepo>();
        _cloudinaryMock = new Mock<ICloudinaryService>();
        _pdfGeneratorMock = new Mock<IContractPdfGenerator>();
        _service = new ManagerEmploymentContractService(_repoMock.Object, _cloudinaryMock.Object, _pdfGeneratorMock.Object);

        // Standard manager setup
        var managerUser = new User
        {
            UserId = _managerId,
            BranchId = _branchId,
            Status = "ACTIVE",
            Role = new Role { RoleName = "MANAGER" }
        };
        _repoMock.Setup(r => r.GetCreationUserAsync(_managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerUser);
    }

    [Fact]
    public async Task CancelContract_WhenContractBelongsToDifferentBranch_ThrowsUnauthorizedAccessException()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = 999, // Different branch
            Status = "DRAFT"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new CancelEmploymentContractRequest { Reason = "Hủy thử nghiệm" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CancelContractAsync(_managerId, 1, request));
    }

    [Fact]
    public async Task CancelContract_WhenActiveStatus_ThrowsInvalidOperationException()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = _branchId,
            Status = "ACTIVE"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _repoMock.Setup(r => r.CancelContractAsync(1, _managerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Không thể hủy hợp đồng ở trạng thái ACTIVE."));

        var request = new CancelEmploymentContractRequest { Reason = "Muốn hủy" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelContractAsync(_managerId, 1, request));
    }

    [Fact]
    public async Task CancelContract_WhenDraftOrPendingStaff_CancelsSuccessfully()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-123",
            BranchId = _branchId,
            Status = "PENDING_STAFF",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Tran Van B"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new CancelEmploymentContractRequest { Reason = "Nhân viên đổi ý không làm việc nữa" };

        var result = await _service.CancelContractAsync(_managerId, 1, request);

        Assert.Equal("CANCELLED", result.Status);
        Assert.Equal("Nhân viên đổi ý không làm việc nữa", result.TerminationReason);
        _repoMock.Verify(r => r.CancelContractAsync(1, _managerId, "Nhân viên đổi ý không làm việc nữa", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TerminateContract_WhenContractNotActive_ThrowsInvalidOperationException()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = _branchId,
            Status = "PENDING_STAFF"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new TerminateEmploymentContractRequest { Reason = "Chấm dứt hợp đồng" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.TerminateContractAsync(_managerId, 1, request));
    }

    [Fact]
    public async Task TerminateContract_WhenActive_TerminatesSuccessfully()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-123",
            BranchId = _branchId,
            Status = "ACTIVE",
            UserId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 1, 1),
            EmployeeFullNameSnapshot = "Tran Van B"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new TerminateEmploymentContractRequest
        {
            Reason = "Vi phạm nội quy nghiêm trọng",
            TerminationEffectiveDate = new DateOnly(2026, 10, 10)
        };

        var result = await _service.TerminateContractAsync(_managerId, 1, request);

        Assert.Equal("TERMINATED", result.Status);
        Assert.Equal("Vi phạm nội quy nghiêm trọng", result.TerminationReason);
        _repoMock.Verify(r => r.TerminateContractAsync(1, _managerId, "Vi phạm nội quy nghiêm trọng", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveChangeRequest_WhenChangeRequestNotSubmitted_ThrowsInvalidOperationException()
    {
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 10,
            ContractId = 1,
            Status = "REJECTED",
            Contract = new EmploymentContract { ContractId = 1, BranchId = _branchId }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);

        var request = new ApproveContractChangeRequest { ReviewComment = "Đồng ý" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveChangeRequestAsync(_managerId, 10, request));
    }

    [Fact]
    public async Task ApproveChangeRequest_WhenWrongBranch_ThrowsUnauthorizedAccessException()
    {
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 10,
            ContractId = 1,
            Status = "SUBMITTED",
            Contract = new EmploymentContract { ContractId = 1, BranchId = 999 } // Different branch
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);

        var request = new ApproveContractChangeRequest { ReviewComment = "Đồng ý" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ApproveChangeRequestAsync(_managerId, 10, request));
    }

    [Fact]
    public async Task ApproveChangeRequest_WhenValid_ApprovesAndNotifiesStaff()
    {
        var staffUserId = Guid.NewGuid();
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 10,
            ContractId = 1,
            Status = "SUBMITTED",
            RequestedBy = staffUserId,
            Contract = new EmploymentContract
            {
                ContractId = 1,
                ContractNumber = "EC-123",
                BranchId = _branchId,
                HourlyRate = 25000,
                ContractTemplateVersion = "1.0"
            }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);

        var request = new ApproveContractChangeRequest { ReviewComment = "Đã duyệt điều chỉnh" };

        var result = await _service.ApproveChangeRequestAsync(_managerId, 10, request);

        Assert.Equal("APPROVED", result.Status);
        Assert.Equal("Đã duyệt điều chỉnh", result.ReviewComment);
        _repoMock.Verify(r => r.ApproveChangeRequestWithContractUpdateAsync(10, _managerId, "Đã duyệt điều chỉnh", It.IsAny<EmploymentContract>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveChangeRequest_WhenManagerCustomizesItems_OverridesStaffValuesAndBumpsVersion()
    {
        var staffUserId = Guid.NewGuid();
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 11,
            ContractId = 1,
            Status = "SUBMITTED",
            RequestedBy = staffUserId,
            Contract = new EmploymentContract
            {
                ContractId = 1,
                ContractNumber = "EC-123",
                BranchId = _branchId,
                HourlyRate = 25000,
                EmployeePositionSnapshot = "Nhân viên bán hàng",
                ContractTemplateVersion = "1.0",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31)
            },
            ContractChangeRequestItems = new List<ContractChangeRequestItem>
            {
                new ContractChangeRequestItem
                {
                    ChangeRequestId = 11,
                    FieldName = "HourlyRate",
                    ProposedValue = "30000"
                },
                new ContractChangeRequestItem
                {
                    ChangeRequestId = 11,
                    FieldName = "EmployeePosition",
                    ProposedValue = "Trưởng ca"
                }
            }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);

        var request = new ApproveContractChangeRequest
        {
            ReviewComment = "Duyệt mức lương 28,000 đ/giờ và chức danh Trưởng nhóm",
            Items = new List<ApproveContractChangeRequestItemInputDto>
            {
                new ApproveContractChangeRequestItemInputDto
                {
                    FieldName = "HourlyRate",
                    FinalValue = "28000" // Manager chốt 28000 thay vì Staff đề xuất 30000
                },
                new ApproveContractChangeRequestItemInputDto
                {
                    FieldName = "EmployeePosition",
                    FinalValue = "Trưởng nhóm" // Manager chốt Trưởng nhóm
                }
            }
        };

        var result = await _service.ApproveChangeRequestAsync(_managerId, 11, request);

        Assert.Equal("APPROVED", result.Status);
        Assert.Equal("1.1", cr.Contract.ContractTemplateVersion);
        Assert.Equal(28000m, cr.Contract.HourlyRate);
        Assert.Equal("Trưởng nhóm", cr.Contract.EmployeePositionSnapshot);
        Assert.Equal("PENDING_STAFF", cr.Contract.Status);
        Assert.Null(cr.Contract.StaffAcceptedAt);
        _repoMock.Verify(r => r.ApproveChangeRequestWithContractUpdateAsync(11, _managerId, request.ReviewComment, cr.Contract, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectChangeRequest_WhenMissingReason_ThrowsArgumentException()
    {
        var request = new RejectContractChangeRequest { Reason = "" };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RejectChangeRequestAsync(_managerId, 10, request));
    }

    [Fact]
    public async Task RejectChangeRequest_WhenValid_RejectsSuccessfully()
    {
        var staffUserId = Guid.NewGuid();
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 10,
            ContractId = 1,
            Status = "SUBMITTED",
            RequestedBy = staffUserId,
            Contract = new EmploymentContract
            {
                ContractId = 1,
                ContractNumber = "EC-123",
                BranchId = _branchId
            }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);

        var request = new RejectContractChangeRequest { Reason = "Không phù hợp với nhu cầu chi nhánh" };

        var result = await _service.RejectChangeRequestAsync(_managerId, 10, request);

        Assert.Equal("REJECTED", result.Status);
        Assert.Equal("Không phù hợp với nhu cầu chi nhánh", result.ReviewComment);
        _repoMock.Verify(r => r.RejectChangeRequestAsync(10, _managerId, "Không phù hợp với nhu cầu chi nhánh", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendContract_WhenCloudinaryUploadFails_ThrowsInvalidOperationExceptionAndDoesNotSend()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-123",
            BranchId = _branchId,
            Status = "DRAFT"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _pdfGeneratorMock.Setup(p => p.GeneratePdf(contract, false))
            .Returns(new byte[] { 1, 2, 3 });

        _cloudinaryMock.Setup(c => c.UploadContractPdfAsync("EC-123", It.IsAny<byte[]>()))
            .ThrowsAsync(new InvalidOperationException("Cloudinary API connection error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendContractAsync(_managerId, 1));

        _repoMock.Verify(r => r.SendContractPdfAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetContractHistory_WhenContractNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmploymentContract?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetContractHistoryAsync(_managerId, 99));
    }

    [Fact]
    public async Task GetContractHistory_WhenDifferentBranch_ThrowsUnauthorizedAccessException()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = 999, // Different branch
            Status = "ACTIVE"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetContractHistoryAsync(_managerId, 1));
    }

    [Fact]
    public async Task GetContractHistory_ReturnsPagedHistoryWithParsedAuditLogs()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            ContractNumber = "EC-123",
            BranchId = _branchId,
            BranchNameSnapshot = "Chi nhánh Quận 1",
            Status = "ACTIVE",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Pham Van C",
            CreatedBy = _managerId,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            SentToStaffAt = DateTime.UtcNow.AddDays(-8),
            StaffAcceptedAt = DateTime.UtcNow.AddDays(-7)
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.GetSuccessorContractAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmploymentContract?)null);

        var managerUser = new User { UserId = _managerId, FullName = "Nguyen Manager", Role = new Role { RoleName = "MANAGER" } };
        var staffUser = new User { UserId = contract.UserId, FullName = "Pham Van C", Role = new Role { RoleName = "STAFF" } };

        var auditLogs = new List<AuditLog>
        {
            new()
            {
                AuditLogId = 101,
                UserId = _managerId,
                User = managerUser,
                Action = "CREATE_DRAFT",
                TableName = "employment_contracts",
                RecordId = "1",
                OldValue = null,
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = (string?)null,
                    toStatus = "DRAFT",
                    startDate = "2026-03-01",
                    endDate = "2027-03-01",
                    hourlyRate = 25000m,
                    note = "Tạo dự thảo hợp đồng lao động mới."
                }),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                AuditLogId = 102,
                UserId = _managerId,
                User = managerUser,
                Action = "UPDATE_DRAFT",
                TableName = "employment_contracts",
                RecordId = "1",
                OldValue = JsonSerializer.Serialize(new
                {
                    startDate = "2026-03-01",
                    endDate = "2027-03-01",
                    hourlyRate = 25000m
                }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = "DRAFT",
                    toStatus = "DRAFT",
                    startDate = "2026-03-05",
                    endDate = "2027-03-05",
                    hourlyRate = 28000m,
                    note = "Cập nhật nội dung dự thảo hợp đồng."
                }),
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            },
            new()
            {
                AuditLogId = 103,
                UserId = _managerId,
                User = managerUser,
                Action = "SEND_CONTRACT",
                TableName = "employment_contracts",
                RecordId = "1",
                OldValue = JsonSerializer.Serialize(new { fromStatus = "DRAFT" }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = "DRAFT",
                    toStatus = "PENDING_STAFF",
                    contractFilePath = "https://res.cloudinary.com/demo/image/upload/sample.pdf",
                    contractTemplateVersion = "1.0",
                    note = "Tạo PDF và gửi hợp đồng chính thức phiên bản 1.0 cho nhân viên."
                }),
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new()
            {
                AuditLogId = 104,
                UserId = contract.UserId,
                User = staffUser,
                Action = "SUBMIT_CHANGE_REQUEST",
                TableName = "employment_contracts",
                RecordId = "1",
                OldValue = JsonSerializer.Serialize(new { fromStatus = "PENDING_STAFF" }),
                NewValue = JsonSerializer.Serialize(new
                {
                    changeRequestId = 55,
                    fromStatus = "PENDING_STAFF",
                    toStatus = "PENDING_MANAGER_APPROVAL",
                    reason = "Đề xuất tăng mức lương",
                    items = new[]
                    {
                        new
                        {
                            fieldName = "hourlyRate",
                            originalValue = "28000",
                            proposedValue = "30000",
                            reason = "Kinh nghiệm làm việc trước đây"
                        }
                    }
                }),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new()
            {
                AuditLogId = 105,
                UserId = null, // System event
                User = null,
                Action = "AUTO_ACTIVATE_CONTRACT",
                TableName = "employment_contracts",
                RecordId = "1",
                OldValue = JsonSerializer.Serialize(new { fromStatus = "SCHEDULED" }),
                NewValue = JsonSerializer.Serialize(new
                {
                    fromStatus = "SCHEDULED",
                    toStatus = "ACTIVE",
                    reason = "Hệ thống tự động kích hoạt hợp đồng khi đến ngày bắt đầu.",
                    note = "Hợp đồng đã có hiệu lực (ACTIVE)."
                }),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(1, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((auditLogs, 5));

        var result = await _service.GetContractHistoryAsync(_managerId, 1, 1, 20, "desc");

        Assert.NotNull(result);
        Assert.Equal(1, result.ContractId);
        Assert.Equal("EC-123", result.ContractNumber);
        Assert.Equal("ACTIVE", result.CurrentStatus);
        Assert.Equal("Pham Van C", result.StaffFullName);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(5, result.Items.Count);

        // Check entry 101: CREATE_DRAFT
        var createEntry = result.Items.First(i => i.AuditLogId == 101);
        Assert.Equal("CREATE_DRAFT", createEntry.Action);
        Assert.Equal("Tạo dự thảo hợp đồng", createEntry.ActionTitle);
        Assert.Equal("DRAFT", createEntry.ToStatus);
        Assert.Equal("Nguyen Manager", createEntry.ActorName);
        Assert.Equal("MANAGER", createEntry.ActorRole);

        // Check entry 102: UPDATE_DRAFT with changed fields
        var updateEntry = result.Items.First(i => i.AuditLogId == 102);
        Assert.Equal("UPDATE_DRAFT", updateEntry.Action);
        Assert.NotNull(updateEntry.ChangedFields);
        Assert.Contains(updateEntry.ChangedFields, f => f.FieldName == "Ngày bắt đầu" && f.NewValue == "2026-03-05");
        Assert.Contains(updateEntry.ChangedFields, f => f.FieldName == "Mức lương giờ" && f.NewValue == "28000");

        // Check entry 103: SEND_CONTRACT
        var sendEntry = result.Items.First(i => i.AuditLogId == 103);
        Assert.Equal("SEND_CONTRACT", sendEntry.Action);
        Assert.Equal("1.0", sendEntry.TemplateVersion);
        Assert.Equal("https://res.cloudinary.com/demo/image/upload/sample.pdf", sendEntry.ContractFilePath);
        Assert.Equal("PENDING_STAFF", sendEntry.ToStatus);

        // Check entry 104: SUBMIT_CHANGE_REQUEST with items diff
        var crEntry = result.Items.First(i => i.AuditLogId == 104);
        Assert.Equal("SUBMIT_CHANGE_REQUEST", crEntry.Action);
        Assert.Equal(55, crEntry.ChangeRequestId);
        Assert.Equal("Đề xuất tăng mức lương", crEntry.Reason);
        Assert.NotNull(crEntry.ChangeRequestItems);
        Assert.Single(crEntry.ChangeRequestItems);
        Assert.Equal("Mức lương giờ", crEntry.ChangeRequestItems[0].FieldName);
        Assert.Equal("28000", crEntry.ChangeRequestItems[0].OriginalValue);
        Assert.Equal("30000", crEntry.ChangeRequestItems[0].ProposedValue);

        // Check entry 105: AUTO_ACTIVATE_CONTRACT (System worker actor)
        var autoEntry = result.Items.First(i => i.AuditLogId == 105);
        Assert.Equal("AUTO_ACTIVATE_CONTRACT", autoEntry.Action);
        Assert.Null(autoEntry.ActorId);
        Assert.Equal("Hệ thống", autoEntry.ActorName);
        Assert.Equal("SYSTEM", autoEntry.ActorRole);
        Assert.Equal("SCHEDULED", autoEntry.FromStatus);
        Assert.Equal("ACTIVE", autoEntry.ToStatus);
    }

    [Fact]
    public async Task CancelContract_WhenRepoThrows_RollsBackAndRethrows()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = _branchId,
            Status = "DRAFT"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.CancelContractAsync(1, _managerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error during cancellation."));

        var request = new CancelEmploymentContractRequest { Reason = "Lý do hủy" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelContractAsync(_managerId, 1, request));
    }

    [Fact]
    public async Task TerminateContract_WhenRepoThrows_RollsBackAndRethrows()
    {
        var contract = new EmploymentContract
        {
            ContractId = 1,
            BranchId = _branchId,
            Status = "ACTIVE"
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.TerminateContractAsync(1, _managerId, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error during termination."));

        var request = new TerminateEmploymentContractRequest
        {
            Reason = "Lý do chấm dứt",
            TerminationEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.TerminateContractAsync(_managerId, 1, request));
    }

    [Fact]
    public async Task GetContractHistory_WithAscendingSortAndPagination_PassesCorrectParametersToRepo()
    {
        var contract = new EmploymentContract
        {
            ContractId = 5,
            ContractNumber = "EC-555",
            BranchId = _branchId,
            Status = "ACTIVE",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Le Thi D"
        };
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(5, 2, 10, "asc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 25));

        var result = await _service.GetContractHistoryAsync(_managerId, 5, 2, 10, "asc");

        Assert.NotNull(result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages); // 25 items / 10 = 3 pages
        _repoMock.Verify(r => r.GetContractAuditLogsPagedAsync(5, 2, 10, "asc", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContractHistory_WithSuccessorAndPredecessor_MapsLinkedContractNumbers()
    {
        var contract = new EmploymentContract
        {
            ContractId = 10,
            ContractNumber = "EC-CURRENT",
            BranchId = _branchId,
            Status = "ACTIVE",
            UserId = Guid.NewGuid(),
            EmployeeFullNameSnapshot = "Tran Thi E",
            ReplacesContractId = 9
        };
        var predecessor = new EmploymentContract { ContractId = 9, ContractNumber = "EC-OLD" };
        var successor = new EmploymentContract { ContractId = 11, ContractNumber = "EC-NEW" };

        _repoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(predecessor);
        _repoMock.Setup(r => r.GetSuccessorContractAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successor);
        _repoMock.Setup(r => r.GetContractAuditLogsPagedAsync(10, 1, 20, "desc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0));

        var result = await _service.GetContractHistoryAsync(_managerId, 10);

        Assert.NotNull(result);
        Assert.Equal(9, result.ReplacesContractId);
        Assert.Equal("EC-OLD", result.ReplacesContractNumber);
        Assert.Equal(11, result.ReplacedByContractId);
        Assert.Equal("EC-NEW", result.ReplacedByContractNumber);
    }

    [Fact]
    public async Task ApproveChangeRequest_WhenRepoThrows_RethrowsException()
    {
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 1,
            ContractId = 10,
            Status = "SUBMITTED",
            Contract = new EmploymentContract { ContractId = 10, BranchId = _branchId }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);
        _repoMock.Setup(r => r.ApproveChangeRequestWithContractUpdateAsync(1, _managerId, It.IsAny<string?>(), It.IsAny<EmploymentContract>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveChangeRequestAsync(_managerId, 1, new ApproveContractChangeRequest()));
    }

    [Fact]
    public async Task RejectChangeRequest_WhenRepoThrows_RethrowsException()
    {
        var cr = new ContractChangeRequest
        {
            ChangeRequestId = 2,
            ContractId = 10,
            Status = "SUBMITTED",
            Contract = new EmploymentContract { ContractId = 10, BranchId = _branchId }
        };
        _repoMock.Setup(r => r.GetChangeRequestByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cr);
        _repoMock.Setup(r => r.RejectChangeRequestAsync(2, _managerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RejectChangeRequestAsync(_managerId, 2, new RejectContractChangeRequest { Reason = "Lý do" }));
    }
}
