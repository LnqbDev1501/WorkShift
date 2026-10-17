using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftServer.API.Controllers.Accountant;

public static class ApiChecks
{
    public static async Task RunAsync()
    {
        var f = new Fixture(); var b = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(PayrollFeedbackController).Assembly.GetName().Name });
        b.Logging.ClearProviders();
        b.Services.AddControllers().AddApplicationPart(typeof(PayrollFeedbackController).Assembly);
        b.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, PayrollTestAuth>("Test", _ => { });
        b.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("ADMIN")));
        b.Services.AddSingleton<IPayrollFeedbackService>(f.Reviews);
        b.Services.AddSingleton<IPayrollCalculationService>(f.Calculation);
        b.Services.AddSingleton<IPayrollCalculationRepo>(f);
        b.Services.AddSingleton<IPayrollRevisionService>(new PayrollRevisionService(f, new RevisionReadFixture(f), f.Reviews));
        var app = b.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        app.Urls.Add("http://127.0.0.1:5459"); await app.StartAsync();
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("HTTP CHECK FAILED: " + label); Console.WriteLine($"HTTP PASS {++count}: {label}"); }
        HttpClient Client(string? role, Guid? actor)
        {
            var c = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5459") };
            if (role != null) c.DefaultRequestHeaders.Add("X-Role", role);
            if (actor.HasValue) c.DefaultRequestHeaders.Add("X-Actor", actor.ToString());
            return c;
        }
        try
        {
            using var accountant = Client("ACCOUNTANT", f.Accountant); using var staff = Client("STAFF", f.Staff);
            using var manager = Client("MANAGER", f.Manager); using var anonymous = Client(null, null);
            Check((await anonymous.GetAsync("/api/payroll-previews")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous denied");
            Check((await staff.PostAsJsonAsync("/api/payroll-periods/1/calculate", new { })).StatusCode == HttpStatusCode.Forbidden, "Staff cannot calculate");
            Check((await manager.PostAsJsonAsync("/api/payroll-periods/1/send-previews", new { })).StatusCode == HttpStatusCode.Forbidden, "Manager cannot send previews");
            var r = await accountant.PostAsJsonAsync("/api/payroll-periods/1/calculate", new { }); Check(r.StatusCode == HttpStatusCode.OK, "Actual calculate route works");
            r = await accountant.PostAsJsonAsync("/api/payroll-periods/1/send-previews", new { }); Check(r.StatusCode == HttpStatusCode.OK, "Actual send-preview route works");
            var id = f.State.Feedback.Single(fb => fb.EmployeeId == f.Staff).FeedbackId;
            Check((await staff.GetAsync($"/api/payroll-previews/{id}")).StatusCode == HttpStatusCode.OK, "Staff reads own preview");
            var other = f.State.Feedback.Single(fb => fb.EmployeeId == f.OtherStaff).FeedbackId;
            Check((await staff.GetAsync($"/api/payroll-previews/{other}")).StatusCode == HttpStatusCode.Forbidden, "Staff cannot read another preview");
            Check((await staff.PostAsJsonAsync($"/api/payroll-previews/{id}/feedback", new { description = "" })).StatusCode == HttpStatusCode.BadRequest, "API validates feedback description");
            Check((await staff.PostAsJsonAsync($"/api/payroll-previews/{id}/feedback", new { description = "Please check reward" })).StatusCode == HttpStatusCode.OK, "Staff submits actual preview feedback");
            Check((await staff.PostAsJsonAsync($"/api/payroll-feedback-requests/{id}/approve", new { resolutionNote = "Spoof" })).StatusCode == HttpStatusCode.Forbidden, "Staff cannot approve own feedback");
            Check((await accountant.PostAsJsonAsync($"/api/payroll-feedback-requests/{id}/approve", new { resolutionNote = "Accepted" })).StatusCode == HttpStatusCode.OK, "Accountant approval creates correction");
            var issue = f.State.Issues.Single();
            Check((await manager.PostAsJsonAsync($"/api/payroll-correction-requests/{issue.CorrectionRequestId}/respond", new { action = "REJECT", note = "No" })).StatusCode == HttpStatusCode.BadRequest, "Manager has no REJECT action");
            Check((await manager.PostAsJsonAsync($"/api/payroll-correction-requests/{issue.CorrectionRequestId}/respond", new { action = "EXPLAIN", note = "Source explanation" })).StatusCode == HttpStatusCode.OK, "Manager explains through existing case");
            Check((await accountant.PostAsJsonAsync($"/api/payroll-correction-requests/{issue.CorrectionRequestId}/review", new { decision = "ACCEPT_EXPLANATION", note = "Accepted explanation" })).StatusCode == HttpStatusCode.OK, "Accountant accepts explanation and sends next round");
            Check((await accountant.PostAsJsonAsync($"/api/payroll-periods/1/employees/{f.Staff}/calculate", new { })).StatusCode == HttpStatusCode.NotFound, "Old individual initial-calculation route removed");
            Check((await staff.PostAsJsonAsync("/api/payslips/1/feedback-requests", new { description = "Old flow" })).StatusCode == HttpStatusCode.NotFound, "Old official-payslip feedback route removed");
            Check((await accountant.PostAsJsonAsync("/api/payrolls/1/approve-correction", new { })).StatusCode == HttpStatusCode.NotFound, "Old post-publication approval route removed");
            Check((await accountant.PostAsJsonAsync("/api/accountant/payroll-periods/1/payrolls/1/submit", new { })).StatusCode == HttpStatusCode.NotFound, "Old individual submission route removed");
            Check((await accountant.PostAsJsonAsync("/api/admin/payroll-periods/1/payrolls/1/reject", new { reason = "Old flow" })).StatusCode == HttpStatusCode.NotFound, "Old Admin rejection route removed");
            Console.WriteLine($"ALL {count} HTTP CHECKS PASSED");
        }
        finally { await app.StopAsync(); await app.DisposeAsync(); }
    }
    private sealed class RevisionReadFixture(Fixture fixture) : IPayrollRevisionRepo
    {
        public Task<WorkShiftManagement.DAL.Entities.Payroll?> GetAsync(int id, int? branchId, CancellationToken ct) => fixture.GetPayrollAsync(id, branchId, ct);
        public Task<List<WorkShiftManagement.DAL.Entities.Payroll>> VersionsAsync(Guid userId, int periodId, CancellationToken ct) => Task.FromResult(fixture.State.Payrolls.Where(p => p.UserId == userId).ToList());
    }
}
public sealed class PayrollTestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Role", out var role)) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[] { new Claim(ClaimTypes.Role, role.ToString()), new Claim(ClaimTypes.NameIdentifier, Request.Headers["X-Actor"].ToString()) };
        return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test")));
    }
}
