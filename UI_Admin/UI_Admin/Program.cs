using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using UI_Admin.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
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
builder.Services.AddHttpClient<RegionApiService>();
builder.Services.AddHttpClient<PayrollApiService>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<DashboardApiService>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<NotificationApiService>();
builder.Services.AddHttpClient<RegionApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<DashboardApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<NotificationApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<UserApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<BranchApiService>().AddHttpMessageHandler<ApiBearerTokenHandler>();
builder.Services.AddHttpClient<ShiftTemplateApiService>(client => client.Timeout = TimeSpan.FromSeconds(15)).AddHttpMessageHandler<ApiBearerTokenHandler>();



var app = builder.Build();

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
