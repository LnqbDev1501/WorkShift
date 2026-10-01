using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftServer.API.Controllers.Manager;
using Xunit;

namespace WorkShiftManagement.Tests;

public class ManagerEmploymentContractTests
{
    private readonly Mock<IEmploymentContractRepo> repo = new();
    private readonly User manager = new() { UserId = Guid.NewGuid(), BranchId = 3, Status = "ACTIVE", Role = new() { RoleName = "MANAGER", Status = "ACTIVE" } };
    private readonly User staff = new()
    {
        UserId = Guid.NewGuid(), BranchId = 3, Status = "PENDING", Role = new() { RoleName = "STAFF" },
        FullName = "Nguyen Van A", Phone = "0901234567", Address = "Staff address", IdentityNo = "012345678901",
        Branch = new() { BranchId = 3, BranchName = "Branch A", Address = "Branch address", Status = "ACTIVE", RegionId = 5,
            Region = new() { RegionId = 5, RegionName = "Region A", IsActive = true, RegionalMinimumWageId = 9,
                RegionalMinimumWage = new() { RegionalMinimumWageId = 9, Status = "ACTIVE", MinimumHourlyRate = 25000,
                    EffectiveFrom = new DateOnly(2025, 1, 1), EffectiveTo = new DateOnly(2027, 1, 1) } } }
    };
    private EmploymentContract? saved;
    private CreateEmploymentContractRequest Request() => new() { StaffUserId = staff.UserId, StartDate = new(2026, 1, 1), EmployeePosition = " Thu ngân ", ContractTerms = "Terms" };
    private ManagerEmploymentContractService Service() => new(repo.Object);
    public ManagerEmploymentContractTests()
    {
        repo.Setup(r => r.GetCreationUserAsync(manager.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        repo.Setup(r => r.GetCreationUserAsync(staff.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        repo.Setup(r => r.CreateDraftAsync(It.IsAny<EmploymentContract>(), It.IsAny<CancellationToken>()))
            .Callback<EmploymentContract, CancellationToken>((c, _) => { Assert.Equal(0, c.ContractId); saved = c; c.ContractId = 42; })
            .Returns(Task.CompletedTask);
    }
    [Theory]
    [InlineData("PENDING")]
    [InlineData("ACTIVE")]
    public async Task CreatesDraftAndCopiesSnapshots(string status)
    {
        staff.Status = status;
        var response = await Service().CreateAsync(manager.UserId, Request());
        Assert.Equal(42, response.ContractId);
        Assert.NotNull(saved);
        Assert.Equal("DRAFT", saved.Status);
        Assert.Equal(staff.UserId, saved.UserId);
        Assert.Equal(manager.UserId, saved.CreatedBy);
        Assert.Equal(staff.FullName, saved.EmployeeFullNameSnapshot);
        Assert.Equal(staff.Phone, saved.EmployeePhoneSnapshot);
        Assert.Equal(staff.Address, saved.EmployeeAddressSnapshot);
        Assert.Equal(staff.IdentityNo, saved.EmployeeIdentityNoSnapshot);
        Assert.Equal("Branch A", saved.BranchNameSnapshot);
        Assert.Equal("Branch address", saved.BranchAddressSnapshot);
        Assert.Equal("Region A", saved.RegionNameSnapshot);
        Assert.Equal(3, saved.BranchId); Assert.Equal(5, saved.RegionId);
        Assert.Equal(25000m, saved.HourlyRate);
        Assert.Equal("Thu ngân", saved.EmployeePositionSnapshot);
        Assert.Equal("Terms", saved.ContractTerms);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
        Assert.Null(saved.EndDate); Assert.Null(saved.ReplacesContractId);
        Assert.Null(saved.SentToStaffAt); Assert.Null(saved.StaffAcceptedAt); Assert.Null(saved.StaffRespondedAt);
        Assert.Null(saved.ApprovedAt); Assert.Null(saved.ApprovedBy); Assert.Null(saved.RejectedBy); Assert.Null(saved.RejectedAt);
        Assert.Null(saved.TerminatedAt); Assert.Null(saved.TerminatedBy); Assert.Null(saved.ContractFilePath); Assert.Null(saved.PdfGeneratedAt);
        Assert.Empty(saved.StaffSalaryHistories);
        staff.Branch!.Region.RegionalMinimumWage!.MinimumHourlyRate = 99999;
        staff.FullName = "Changed";
        Assert.Equal(25000m, saved.HourlyRate); Assert.Equal("Nguyen Van A", saved.EmployeeFullNameSnapshot);
    }
    [Theory]
    [InlineData("ADMIN")][InlineData("STAFF")][InlineData("ACCOUNTANT")]
    public async Task RejectsNonManager(string role)
    {
        manager.Role.RoleName = role;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().CreateAsync(manager.UserId, Request()));
        Assert.Null(saved);
    }
    [Fact] public async Task RejectsDifferentBranch()
    {
        staff.BranchId = 4;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().CreateAsync(manager.UserId, Request()));
        Assert.Null(saved);
    }
    [Fact] public async Task RejectsMissingStaff()
    {
        var request = Request(); request.StaffUserId = Guid.NewGuid();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().CreateAsync(manager.UserId, request));
    }
    [Theory]
    [InlineData("missing")][InlineData("inactive")][InlineData("before")][InlineData("after")][InlineData("no-start")][InlineData("zero")]
    public async Task RejectsInvalidWage(string scenario)
    {
        var region = staff.Branch!.Region;
        var wage = region.RegionalMinimumWage!;
        switch (scenario)
        {
            case "missing": region.RegionalMinimumWageId = null; break;
            case "inactive": wage.Status = "INACTIVE"; break;
            case "before": wage.EffectiveFrom = new(2026, 1, 2); break;
            case "after": wage.EffectiveTo = new(2025, 12, 31); break;
            case "no-start": wage.EffectiveFrom = null; break;
            case "zero": wage.MinimumHourlyRate = 0; break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(manager.UserId, Request()));
        Assert.Null(saved);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task AllowsInclusiveWageBounds(bool end)
    {
        var request = Request();
        request.StartDate = end ? staff.Branch!.Region.RegionalMinimumWage!.EffectiveTo!.Value : staff.Branch!.Region.RegionalMinimumWage!.EffectiveFrom!.Value;
        await Service().CreateAsync(manager.UserId, request);
        Assert.NotNull(saved);
    }
    [Theory][InlineData("date")][InlineData("equal")][InlineData("earlier")][InlineData("blank")][InlineData("long")]
    public async Task RejectsInvalidRequest(string scenario)
    {
        var request = Request();
        switch (scenario)
        {
            case "date": request.StartDate = default; break;
            case "equal": request.EndDate = request.StartDate; break;
            case "earlier": request.EndDate = request.StartDate.AddDays(-1); break;
            case "blank": request.EmployeePosition = "   "; break;
            case "long": request.EmployeePosition = new string('a', 101); break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(manager.UserId, request));
        Assert.Null(saved);
    }
    [Theory][InlineData("staff-inactive")][InlineData("branch-inactive")][InlineData("region-inactive")][InlineData("staff-role")]
    public async Task RejectsIneligibleStaffOrLocation(string scenario)
    {
        switch (scenario)
        {
            case "staff-inactive": staff.Status = "INACTIVE"; break;
            case "branch-inactive": staff.Branch!.Status = "INACTIVE"; break;
            case "region-inactive": staff.Branch!.Region.IsActive = false; break;
            case "staff-role": staff.Role.RoleName = "ADMIN"; break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(manager.UserId, Request()));
        Assert.Null(saved);
    }
    [Fact] public async Task HonorsExistingPendingConstraint()
    {
        repo.Setup(r => r.HasPendingContractAsync(staff.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().CreateAsync(manager.UserId, Request()));
        Assert.Null(saved);
    }
    [Fact] public async Task GeneratesDistinctNumbers()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Service().CreateAsync(manager.UserId, Request())));
        Assert.Equal(50, responses.Select(r => r.ContractNumber).Distinct().Count());
        Assert.All(responses, r => Assert.InRange(r.ContractNumber.Length, 1, 50));
    }
    [Fact] public async Task ControllerReturns201AndRequiresManagerRole()
    {
        var attribute = Assert.Single(typeof(ManagerEmploymentContractController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("MANAGER", attribute.Roles);
        var controller = new ManagerEmploymentContractController(Service(), NullLogger<ManagerEmploymentContractController>.Instance)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", manager.UserId.ToString()) }, "test")) } }
        };
        var result = await controller.Create(Request(), default);
        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }
    [Fact] public async Task IgnoresClientControlledServerFields()
    {
        var request = JsonSerializer.Deserialize<CreateEmploymentContractRequest>(
            $$"""{"staffUserId":"{{staff.UserId}}","startDate":"2026-01-01","employeePosition":"Cashier","status":"ACTIVE","hourlyRate":1,"branchId":999,"createdBy":"{{staff.UserId}}"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        await Service().CreateAsync(manager.UserId, request);
        Assert.Equal("DRAFT", saved!.Status); Assert.Equal(25000m, saved.HourlyRate);
        Assert.Equal(3, saved.BranchId); Assert.Equal(manager.UserId, saved.CreatedBy);
    }
}
