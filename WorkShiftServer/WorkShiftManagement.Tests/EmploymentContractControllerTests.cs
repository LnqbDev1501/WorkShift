using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftServer.API.Controllers.Manager;
using Xunit;

namespace WorkShiftManagement.Tests;

public class EmploymentContractControllerTests
{
    [Theory]
    [InlineData(400)][InlineData(403)][InlineData(404)][InlineData(409)][InlineData(500)]
    public async Task MapsBusinessErrors(int status)
    {
        Exception error = status switch
        {
            400 => new ArgumentException("Invalid date"),
            403 => new UnauthorizedAccessException("Wrong branch"),
            404 => new KeyNotFoundException("Staff does not exist"),
            409 => new InvalidOperationException("Pending contract exists"),
            _ => new Exception("Internal database details")
        };
        var service = new Mock<IManagerEmploymentContractService>();
        service.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateEmploymentContractRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
        var controller = new ManagerEmploymentContractController(service.Object, NullLogger<ManagerEmploymentContractController>.Instance)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "test")) } }
        };
        var result = Assert.IsAssignableFrom<ObjectResult>((await controller.Create(new(), default)).Result);
        Assert.Equal(status, result.StatusCode);
        var message = result.Value!.GetType().GetProperty("message")!.GetValue(result.Value);
        if (status == 500) Assert.DoesNotContain("Internal database details", message!.ToString());
        else Assert.Equal(error.Message, message);
    }
    [Fact]
    public async Task MissingIdentityReturns401WithoutCallingService()
    {
        var service = new Mock<IManagerEmploymentContractService>(MockBehavior.Strict);
        var controller = new ManagerEmploymentContractController(service.Object, NullLogger<ManagerEmploymentContractController>.Instance)
        { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedObjectResult>((await controller.Create(new(), default)).Result);
    }
    [Fact]
    public async Task AdminDetailStillReadsSnapshot()
    {
        var repo = new Mock<IEmploymentContractRepo>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new EmploymentContract
        {
            ContractId = 1, ContractNumber = "EC-existing", EmployeeFullNameSnapshot = "Original name",
            EmployeePositionSnapshot = "Cashier", BranchNameSnapshot = "Original branch", RegionNameSnapshot = "Original region",
            EmployeePhoneSnapshot = "0901234567", EmployeeAddressSnapshot = "Original address", EmployeeIdentityNoSnapshot = "123456",
            Status = "DRAFT", User = new() { FullName = "Updated name" }
        });
        var response = await new AdminEmploymentContractService(repo.Object).GetByIdAsync(1);
        Assert.Equal("Original name", response.FullName);
        Assert.Equal("EC-existing", response.ContractNumber);
        Assert.Equal("Original branch", response.BranchName);
    }
}
