using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using UI_Admin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ManagerContractApiService>(client => client.Timeout = TimeSpan.FromSeconds(30)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "WorkShift.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddTransient<ApiBearerTokenHandler>();

builder.Services.AddHttpClient<AuthApiService>();
builder.Services.AddHttpClient<PayrollApiService>(client => client.Timeout = TimeSpan.FromSeconds(15))
    .AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<WorkingTimeRuleApiService>(client => client.Timeout = TimeSpan.FromSeconds(30)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<RegionApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<EmployeeTypeApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<RegionalMinimumWageApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<DashboardApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<NotificationApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<UserApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<BranchApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<ShiftTemplateApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<CategoryApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<ProductApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<ManagerStaffApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<CategoryApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<ProductApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<HolidayApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<EmploymentContractApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<AttendanceRuleApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<EmployeeRequestApiService>(client => client.Timeout = TimeSpan.FromSeconds(20)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<IVietMapService, VietMapService>(client => client.Timeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
