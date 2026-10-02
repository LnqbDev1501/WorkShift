using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI_Admin.Controllers;
using UI_Admin.Models.Manager;
using UI_Admin.Services;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftServer.API.Controllers.Manager;
using Xunit;

namespace WorkShiftManagement.Tests;

public class ManagerContractCrudTests
{
    private readonly Mock<IEmploymentContractRepo> repo = new();
    private readonly User manager = new()
    {
        UserId = Guid.NewGuid(),
        BranchId = 3,
        Status = "ACTIVE",
        Role = new() { RoleName = "MANAGER", Status = "ACTIVE" }
    };

    private readonly Region activeRegion = new()
    {
        RegionId = 5,
        RegionName = "Vùng 1",
        IsActive = true,
        RegionalMinimumWageId = 10,
        RegionalMinimumWage = new()
        {
            RegionalMinimumWageId = 10,
            Status = "ACTIVE",
            MinimumHourlyRate = 27000,
            EffectiveFrom = new DateOnly(2025, 1, 1),
            EffectiveTo = new DateOnly(2027, 12, 31)
        }
    };

    private EmploymentContract CreateTestContract(int id = 100, int branchId = 3, string status = "DRAFT")
    {
        return new EmploymentContract
        {
            ContractId = id,
            ContractNumber = "EC-test100",
            UserId = Guid.NewGuid(),
            BranchId = branchId,
            RegionId = 5,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2027, 3, 1),
            HourlyRate = 25000,
            EmployeePositionSnapshot = "Nhân viên kho",
            EmployeeFullNameSnapshot = "Trần Thị B",
            EmployeePhoneSnapshot = "0987654321",
            EmployeeAddressSnapshot = "123 Đường ABC",
            EmployeeIdentityNoSnapshot = "098765432101",
            BranchNameSnapshot = "Chi nhánh Quận 1",
            BranchAddressSnapshot = "456 Đường XYZ",
            RegionNameSnapshot = "Vùng 1",
            ContractTerms = "Điều khoản mặc định",
            Status = status,
            CreatedBy = manager.UserId,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Region = activeRegion,
            Branch = new() { BranchId = branchId, BranchName = "Chi nhánh Quận 1", Status = "ACTIVE", Region = activeRegion }
        };
    }

    private ManagerEmploymentContractService Service() => new(repo.Object);

    public ManagerContractCrudTests()
    {
        repo.Setup(r => r.GetCreationUserAsync(manager.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
    }

    [Fact]
    public async Task GetPaged_FiltersByManagersBranchAndReturnsList()
    {
        var contract1 = CreateTestContract(101, 3);
        var contract2 = CreateTestContract(102, 3);
        repo.Setup(r => r.GetManagerPagedAsync(3, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<EmploymentContract> { contract1, contract2 }, 2));

        var result = await Service().GetPagedAsync(manager.UserId, new ManagerEmploymentContractListRequest());

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("EC-test100", result.Items[0].ContractNumber);
        Assert.Equal("Trần Thị B", result.Items[0].EmployeeFullName);
        Assert.Equal(3, result.Items[0].BranchId);
        repo.Verify(r => r.GetManagerPagedAsync(3, null, null, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsContractDetails_WhenBelongsToBranch()
    {
        var contract = CreateTestContract(100, 3);
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var result = await Service().GetByIdAsync(manager.UserId, 100);

        Assert.Equal(100, result.ContractId);
        Assert.Equal("Trần Thị B", result.EmployeeFullName);
        Assert.Equal("Nhân viên kho", result.EmployeePosition);
        Assert.Equal(25000, result.HourlyRate);
        Assert.Equal("Chi nhánh Quận 1", result.BranchName);
        Assert.Equal("Vùng 1", result.RegionName);
    }

    [Fact]
    public async Task GetById_ThrowsUnauthorized_WhenBelongsToDifferentBranch()
    {
        var contract = CreateTestContract(100, branchId: 99); // branch 99 != manager's branch 3
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Service().GetByIdAsync(manager.UserId, 100));
        Assert.Contains("branch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_ThrowsKeyNotFound_WhenDoesNotExist()
    {
        repo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmploymentContract?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Service().GetByIdAsync(manager.UserId, 999));
    }

    [Fact]
    public async Task UpdateDraft_SucceedsAndUpdatesWage_WhenStartDateChanges()
    {
        var contract = CreateTestContract(100, 3, "DRAFT");
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        EmploymentContract? updated = null;
        repo.Setup(r => r.UpdateDraftAsync(It.IsAny<EmploymentContract>(), manager.UserId, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<EmploymentContract, Guid, object, CancellationToken>((c, _, _, _) => updated = c)
            .Returns(Task.CompletedTask);

        var request = new UpdateEmploymentContractRequest
        {
            StartDate = new DateOnly(2026, 5, 1), // new start date
            EndDate = new DateOnly(2027, 5, 1),
            EmployeePosition = " Giám sát ca ",
            ContractTerms = "Điều khoản sửa đổi"
        };

        var response = await Service().UpdateDraftAsync(manager.UserId, 100, request);

        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 5, 1), updated.StartDate);
        Assert.Equal(new DateOnly(2027, 5, 1), updated.EndDate);
        Assert.Equal("Giám sát ca", updated.EmployeePositionSnapshot);
        Assert.Equal("Điều khoản sửa đổi", updated.ContractTerms);
        Assert.Equal(27000m, updated.HourlyRate); // rechecked wage rate
        Assert.Equal(27000m, response.HourlyRate);
        Assert.Equal("Giám sát ca", response.EmployeePosition);
        // Ensure snapshots were not altered
        Assert.Equal("Trần Thị B", updated.EmployeeFullNameSnapshot);
        Assert.Equal(3, updated.BranchId);
    }

    [Fact]
    public async Task UpdateDraft_ThrowsUnauthorized_WhenWrongBranch()
    {
        var contract = CreateTestContract(100, branchId: 4, status: "DRAFT");
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new UpdateEmploymentContractRequest
        {
            StartDate = new DateOnly(2026, 5, 1),
            EmployeePosition = "Giám sát"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Service().UpdateDraftAsync(manager.UserId, 100, request));
    }

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("CANCELLED")]
    [InlineData("PENDING_STAFF")]
    [InlineData("TERMINATED")]
    public async Task UpdateDraft_ThrowsInvalidOperation_WhenNotDraft(string status)
    {
        var contract = CreateTestContract(100, 3, status);
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new UpdateEmploymentContractRequest
        {
            StartDate = new DateOnly(2026, 5, 1),
            EmployeePosition = "Giám sát"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service().UpdateDraftAsync(manager.UserId, 100, request));
        Assert.Contains("draft", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateDraft_ThrowsArgument_WhenWageConfigMissingOrExpired()
    {
        var contract = CreateTestContract(100, 3, "DRAFT");
        // Expired wage config
        contract.Region.RegionalMinimumWage!.EffectiveTo = new DateOnly(2026, 4, 30);
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var request = new UpdateEmploymentContractRequest
        {
            StartDate = new DateOnly(2026, 5, 1), // after EffectiveTo
            EmployeePosition = "Giám sát"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            Service().UpdateDraftAsync(manager.UserId, 100, request));
        Assert.Contains("minimum wage", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelDraft_Succeeds_AndSetsCancelledStatusAndReason()
    {
        var contract = CreateTestContract(100, 3, "DRAFT");
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        string? cancelledReason = null;
        repo.Setup(r => r.CancelDraftAsync(100, manager.UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<int, Guid, string, CancellationToken>((_, _, r, _) => cancelledReason = r)
            .Returns(Task.CompletedTask);

        var result = await Service().CancelDraftAsync(manager.UserId, 100, new() { Reason = "Nhân viên đổi ý không nhận việc" });

        Assert.Equal("CANCELLED", result.Status);
        Assert.Equal("Nhân viên đổi ý không nhận việc", result.TerminationReason);
        Assert.Equal("Nhân viên đổi ý không nhận việc", cancelledReason);
    }

    [Fact]
    public async Task CancelDraft_ThrowsUnauthorized_WhenWrongBranch()
    {
        var contract = CreateTestContract(100, branchId: 77, status: "DRAFT");
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Service().CancelDraftAsync(manager.UserId, 100, new() { Reason = "Reason" }));
    }

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("CANCELLED")]
    [InlineData("PENDING_STAFF")]
    public async Task CancelDraft_ThrowsInvalidOperation_WhenNotDraft(string status)
    {
        var contract = CreateTestContract(100, 3, status);
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Service().CancelDraftAsync(manager.UserId, 100, new() { Reason = "Reason" }));
        Assert.Contains("draft", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelDraft_ThrowsArgument_WhenReasonIsBlank()
    {
        var contract = CreateTestContract(100, 3, "DRAFT");
        repo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service().CancelDraftAsync(manager.UserId, 100, new() { Reason = "   " }));
    }

    [Fact]
    public async Task Controller_GetPagedAndGetById_Return200()
    {
        var serviceMock = new Mock<IManagerEmploymentContractService>();
        serviceMock.Setup(s => s.GetPagedAsync(manager.UserId, It.IsAny<ManagerEmploymentContractListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BLL.DTOs.Admin.Response.PagedResponse<BLL.DTOs.Manager.Response.ManagerEmploymentContractListItemResponse>());
        serviceMock.Setup(s => s.GetByIdAsync(manager.UserId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BLL.DTOs.Manager.Response.ManagerEmploymentContractDetailResponse { ContractId = 100 });

        var controller = new ManagerEmploymentContractController(serviceMock.Object, NullLogger<ManagerEmploymentContractController>.Instance)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", manager.UserId.ToString()) }, "test")) } }
        };

        var pagedResult = await controller.GetContracts(new(), default);
        Assert.IsType<OkObjectResult>(pagedResult.Result);

        var detailResult = await controller.GetById(100, default);
        Assert.IsType<OkObjectResult>(detailResult.Result);
    }

    [Fact]
    public async Task Controller_UpdateAndCancel_Return200()
    {
        var serviceMock = new Mock<IManagerEmploymentContractService>();
        serviceMock.Setup(s => s.UpdateDraftAsync(manager.UserId, 100, It.IsAny<UpdateEmploymentContractRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BLL.DTOs.Manager.Response.ManagerEmploymentContractDetailResponse { ContractId = 100, Status = "DRAFT" });
        serviceMock.Setup(s => s.CancelDraftAsync(manager.UserId, 100, It.IsAny<CancelEmploymentContractRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BLL.DTOs.Manager.Response.ManagerEmploymentContractDetailResponse { ContractId = 100, Status = "CANCELLED" });

        var controller = new ManagerEmploymentContractController(serviceMock.Object, NullLogger<ManagerEmploymentContractController>.Instance)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", manager.UserId.ToString()) }, "test")) } }
        };

        var updateResult = await controller.Update(100, new() { StartDate = new(2026, 5, 1), EmployeePosition = "Thu ngân" }, default);
        Assert.IsType<OkObjectResult>(updateResult.Result);

        var cancelResult = await controller.Cancel(100, new() { Reason = "Lý do hủy" }, default);
        Assert.IsType<OkObjectResult>(cancelResult.Result);
    }

    [Fact]
    public async Task UiController_EditGet_ReturnsView_WhenDraft()
    {
        var handler = new SimpleHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/manager/contracts/100")
            {
                var detail = new ManagerContractDetailViewModel
                {
                    ContractId = 100,
                    ContractNumber = "EC-100",
                    Status = "DRAFT",
                    EmployeeFullName = "Nhân viên A",
                    EmployeePosition = "Thu ngân",
                    StartDate = new DateOnly(2026, 6, 1),
                    HourlyRate = 25000
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(detail) });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var api = new ManagerContractApiService(new HttpClient(handler), new ConfigurationBuilder().Build());
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "manager@test.com"), new Claim(ClaimTypes.Role, "MANAGER") }, "test")) };
        var controller = new ManagerContractsController(api, new MemoryCache(new MemoryCacheOptions()))
        {
            ControllerContext = new() { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };

        var result = Assert.IsType<ViewResult>(await controller.Edit(100, default));
        var model = Assert.IsType<EditContractViewModel>(result.Model);
        Assert.Equal(100, model.ContractId);
        Assert.Equal("EC-100", model.ContractNumber);
    }

    [Fact]
    public async Task UiController_CancelPost_RedirectsToDetailsOnSuccess()
    {
        var cancelPosted = false;
        var handler = new SimpleHandler(request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/manager/contracts/100/cancel")
            {
                cancelPosted = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ManagerContractDetailViewModel { ContractId = 100, Status = "CANCELLED" }) });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var api = new ManagerContractApiService(new HttpClient(handler), new ConfigurationBuilder().Build());
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "manager@test.com"), new Claim(ClaimTypes.Role, "MANAGER") }, "test")) };
        var controller = new ManagerContractsController(api, new MemoryCache(new MemoryCacheOptions()))
        {
            ControllerContext = new() { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };

        var result = Assert.IsType<RedirectToActionResult>(await controller.Cancel(100, "Lý do hủy hợp lệ", default));
        Assert.Equal("Details", result.ActionName);
        Assert.True(cancelPosted);
        Assert.Equal("Hủy hợp đồng nháp thành công", controller.TempData["SuccessMessage"]);
    }

    private sealed class SimpleHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => respond(request);
    }
}
