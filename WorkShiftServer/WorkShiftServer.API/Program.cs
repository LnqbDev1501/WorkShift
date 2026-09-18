using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Services.Auth;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Repositories.Admin;
using WorkShiftServer.API.Hubs;
using WorkShiftServer.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the access token returned by /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Missing Supabase:Url configuration.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
        options.Audience = "authenticated";
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Admin", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Accountant", policy => policy.RequireRole("ADMIN", "ACCOUNTANT"));
    options.AddPolicy("Staff", policy => policy.RequireRole("ADMIN", "STAFF"));
    options.AddPolicy("Manager", policy => policy.RequireRole("ADMIN", "MANAGER"));
});
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, SupabaseRoleClaimsTransformation>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MyCnn")));
    
builder.Services.AddScoped<IPayrollApprovalRepo,PayrollApprovalRepo>();
builder.Services.AddScoped<IPayrollApprovalService,PayrollApprovalService>();


builder.Services.AddHttpClient<AuthService>(client =>
{
    var serviceRoleKey = builder.Configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Missing Supabase:ServiceRoleKey configuration.");

    client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
}).AddTypedClient<IAuthService>((client, _) =>
{
    var setPasswordUrl = builder.Configuration["AdminUi:SetPasswordUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:SetPasswordUrl configuration.");
    var resetPasswordUrl = builder.Configuration["AdminUi:ResetPasswordUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:ResetPasswordUrl configuration.");
    return new AuthService(client, setPasswordUrl, resetPasswordUrl);
});




builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IAccountSetupEmailSender, ResendAccountSetupEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<IRegionRepo, RegionRepo>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<IBranchRepo, BranchRepo>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IPublicHolidayRepo,PublicHolidayRepo>();
builder.Services.AddScoped<IPublicHolidayService,PublicHolidayService>();

builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Upload.ICloudinaryService, WorkShiftManagement.BLL.Services.Upload.CloudinaryService>();

builder.Services.AddScoped<IShiftTemplateRepo, ShiftTemplateRepo>();
builder.Services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
builder.Services.AddScoped<IDashboardRepo, DashboardRepo>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationRepo, NotificationRepo>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<INotificationEmailSender, ResendNotificationEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
