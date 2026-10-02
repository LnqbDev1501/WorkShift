using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using UI_Admin.Controllers;
using UI_Admin.Models.Manager;
using UI_Admin.Services;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using Xunit;

namespace WorkShiftManagement.Tests;
public class ManagerContractUiTests
{
    private readonly Guid staffId = Guid.NewGuid();
    private CreateContractViewModel Form() => new() { StaffUserId = staffId, StartDate = new(2026, 10, 1), EmployeePosition = " Thu ngân ", ContractTerms = " Terms " };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => respond(request);
    }
    private static ManagerContractsController Controller(Handler handler)
    {
        var api = new ManagerContractApiService(new HttpClient(handler), new ConfigurationBuilder().Build());
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "manager@example.test"), new Claim(ClaimTypes.Role, "MANAGER") }, "test")) };
        return new(api, new MemoryCache(new MemoryCacheOptions()))
        {
            ControllerContext = new() { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }
    [Fact] public async Task SendsOnlyAllowedFieldsAndRedirectsToRefreshSafeConfirmation()
    {
        var posts = 0;
        using var handler = new Handler(async request =>
        {
            Assert.Equal("/api/manager/contracts", request.RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method); posts++;
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var root = json.RootElement;
            Assert.Equal(5, root.EnumerateObject().Count());
            Assert.Equal(staffId.ToString(), root.GetProperty("staffUserId").GetString());
            Assert.Equal("2026-10-01", root.GetProperty("startDate").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("endDate").ValueKind);
            Assert.Equal("Thu ngân", root.GetProperty("employeePosition").GetString());
            Assert.Equal("Terms", root.GetProperty("contractTerms").GetString());
            return new(HttpStatusCode.Created) { Content = JsonContent.Create(new ContractCreatedResponse(42, "EC-123", staffId, "Staff A", 3, "Branch", 5, "Region", new(2026,10,1), null, 25000, "Thu ngân", "Terms", "DRAFT", Guid.NewGuid(), DateTime.UtcNow)) };
        });
        var controller = Controller(handler);
        var redirect = Assert.IsType<RedirectToActionResult>(await controller.Create(Form(), default));
        Assert.Equal("Created", redirect.ActionName);
        Assert.Equal("Tạo hợp đồng nháp thành công", controller.TempData["SuccessMessage"]);
        var key = Assert.IsType<string>(redirect.RouteValues!["key"]);
        var result = Assert.IsType<ViewResult>(controller.Created(key));
        Assert.Equal("DRAFT", Assert.IsType<ContractCreatedResponse>(result.Model).Status);
        Assert.IsType<ViewResult>(controller.Created(key));
        Assert.Equal(1, posts);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "other@example.test") }, "test"));
        Assert.IsType<RedirectToActionResult>(controller.Created(key));
    }
    [Theory]
    [InlineData(400, "Region has no assigned minimum wage configuration.")]
    [InlineData(403, "Staff does not belong to the Manager's branch.")]
    [InlineData(409, "Staff already has a draft or pending employment contract.")]
    public async Task KeepsEnteredDataAndReloadsOptionsOnBusinessError(int status, string message)
    {
        var gets = 0;
        using var handler = new Handler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = JsonContent.Create(new { message }) });
            gets++;
            Assert.Equal("/api/manager/contracts/staff-options", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<ContractStaffOption>()) });
        });
        var controller = Controller(handler); var model = Form();
        var view = Assert.IsType<ViewResult>(await controller.Create(model, default));
        Assert.Same(model, view.Model); Assert.Equal(staffId, model.StaffUserId);
        Assert.Equal(" Terms ", model.ContractTerms); Assert.Equal(1, gets);
        Assert.Contains(controller.ModelState.Values.SelectMany(v => v.Errors), e => e.ErrorMessage == message);
    }
    [Fact] public async Task InvalidModelNeverPosts()
    {
        using var handler = new Handler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new ContractStaffOption(staffId, "Staff A") }) });
        });
        var controller = Controller(handler);
        controller.ModelState.AddModelError("StartDate", "Ngày không hợp lệ");
        var view = Assert.IsType<ViewResult>(await controller.Create(Form(), default));
        Assert.Single(Assert.IsType<CreateContractViewModel>(view.Model).StaffOptions);
    }
    [Theory][InlineData("dates")][InlineData("position")][InlineData("staff")]
    public void ValidatesInput(string scenario)
    {
        var model = Form();
        if (scenario == "dates") model.EndDate = model.StartDate;
        if (scenario == "position") model.EmployeePosition = "   ";
        if (scenario == "staff") model.StaffUserId = Guid.Empty;
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true));
    }
    [Fact] public void RestrictsManagerAndRequiresAntiforgeryWithoutChangingAdmin()
    {
        Assert.Equal("MANAGER", Assert.Single(typeof(ManagerContractsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        Assert.Equal("ADMIN", Assert.Single(typeof(EmploymentContractsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        var post = typeof(ManagerContractsController).GetMethod("Create", [typeof(CreateContractViewModel), typeof(CancellationToken)])!;
        Assert.True(post.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true));
    }
    [Fact] public async Task StaffOptionsUseManagersBranchForRenewal()
    {
        var actor = Guid.NewGuid(); var repo = new Mock<IEmploymentContractRepo>(MockBehavior.Strict);
        repo.Setup(r => r.GetCreationUserAsync(actor, default)).ReturnsAsync(new User { UserId = actor, BranchId = 7, Status = "ACTIVE", Role = new() { RoleName = "MANAGER", Status = "ACTIVE" } });
        repo.Setup(r => r.GetStaffForRenewalAsync(7, default)).ReturnsAsync(new (User, EmploymentContract)[] { (new User { UserId = staffId, FullName = "Staff A" }, new EmploymentContract { ContractId = 10, ContractNumber = "EC-10" }) });
        var options = await new ManagerEmploymentContractService(repo.Object).GetStaffOptionsAsync(actor, "RENEWAL");
        Assert.Equal(staffId, Assert.Single(options).StaffUserId);
        repo.VerifyAll();
    }
    [Fact] public async Task StaffOptionsGetsStaffWithoutContractForNew()
    {
        var actor = Guid.NewGuid(); var repo = new Mock<IEmploymentContractRepo>(MockBehavior.Strict);
        repo.Setup(r => r.GetCreationUserAsync(actor, default)).ReturnsAsync(new User { UserId = actor, BranchId = 7, Status = "ACTIVE", Role = new() { RoleName = "MANAGER", Status = "ACTIVE" } });
        repo.Setup(r => r.GetStaffWithoutContractAsync(default)).ReturnsAsync(new[] { new User { UserId = staffId, FullName = "Staff A" } });
        var options = await new ManagerEmploymentContractService(repo.Object).GetStaffOptionsAsync(actor, "NEW");
        Assert.Equal(staffId, Assert.Single(options).StaffUserId);
        repo.VerifyAll();
    }
    [Fact] public async Task StaffOptionsRejectAdmin()
    {
        var actor = Guid.NewGuid(); var repo = new Mock<IEmploymentContractRepo>();
        repo.Setup(r => r.GetCreationUserAsync(actor, default)).ReturnsAsync(new User { BranchId = 7, Status = "ACTIVE", Role = new() { RoleName = "ADMIN", Status = "ACTIVE" } });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new ManagerEmploymentContractService(repo.Object).GetStaffOptionsAsync(actor));
        repo.Verify(r => r.GetStaffWithoutContractAsync(It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.GetStaffForRenewalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
