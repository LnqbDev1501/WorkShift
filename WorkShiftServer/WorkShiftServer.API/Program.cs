using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Services.Auth;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.BLL.Interfaces.Notification;
using WorkShiftManagement.BLL.Services.Notification;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Repositories.Admin;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Accountant;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Repositories.Accountant;
using WorkShiftManagement.DAL.Interfaces.Staff;
using WorkShiftManagement.DAL.Repositories.Staff;
using WorkShiftManagement.DAL.Interfaces.Notification;
using WorkShiftManagement.DAL.Repositories.Notification;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Repositories.Manager;
using WorkShiftServer.API.Hubs;
using WorkShiftServer.API.Services;
using WorkShiftServer.API.Services.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<AdminDataChangedFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<AdminDataChangedFilter>());
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

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

var myCnn = builder.Configuration.GetConnectionString("MyCnn")
    ?? throw new InvalidOperationException("Missing MyCnn connection string.");
// The Supabase pooler allows only 15 session clients: bound client-side pooling
// and Hangfire workers so background jobs cannot starve web requests.
var pooledConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(myCnn) { MaxPoolSize = 8 }.ToString();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(pooledConnectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire") ?? pooledConnectionString;
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
});

builder.Services.AddScoped<IPayrollApprovalRepo, PayrollApprovalRepo>();
builder.Services.AddScoped<IPayrollApprovalService, PayrollApprovalService>();
builder.Services.AddScoped<ISalaryHistoryRepo, SalaryHistoryRepo>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<IPayrollPeriodRepo, PayrollPeriodRepo>();
builder.Services.AddScoped<IPayrollPeriodService, PayrollPeriodService>();
builder.Services.AddScoped<IPayrollCalculationRepo, PayrollCalculationRepo>();
builder.Services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();

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
    var staffSetupUrl = builder.Configuration["AdminUi:StaffSetupUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:StaffSetupUrl configuration.");
    return new AuthService(client, setPasswordUrl, resetPasswordUrl, staffSetupUrl);
});

builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<ProfileEmailChangeTokenService>();
builder.Services.AddSingleton<StaffSetupTokenService>();
builder.Services.AddHttpClient<IAccountSetupEmailSender, ResendAccountSetupEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddHttpClient<IStaffSetupEmailSender, ResendStaffSetupEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddHttpClient<IProfileEmailChangeSender, ResendProfileEmailChangeSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<IWorkingTimeRuleRepo, WorkingTimeRuleRepo>();
builder.Services.AddScoped<IWorkingTimeRuleService, WorkingTimeRuleService>();
builder.Services.AddScoped<IAttendanceRuleRepo, AttendanceRuleRepo>();
builder.Services.AddScoped<IAttendanceRuleService, AttendanceRuleService>();
builder.Services.AddScoped<IAttendancePenaltyRuleRepo, AttendancePenaltyRuleRepo>();
builder.Services.AddScoped<IAttendancePenaltyRuleService, AttendancePenaltyRuleService>();
builder.Services.AddScoped<IRegionRepo, RegionRepo>();

builder.Services.AddScoped<IEmployeeTypeRepo, EmployeeTypeRepo>();
builder.Services.AddScoped<IEmployeeTypeService, EmployeeTypeService>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<IRegionalMinimumWageRepo, RegionalMinimumWageRepo>();
builder.Services.AddScoped<IRegionalMinimumWageService, RegionalMinimumWageService>();
builder.Services.AddHostedService<RegionalMinimumWageActivationWorker>();
builder.Services.AddScoped<IEmploymentContractRepo, EmploymentContractRepo>();
builder.Services.AddScoped<IAdminEmploymentContractService, AdminEmploymentContractService>();
builder.Services.AddScoped<IAdminEmployeeRequestRepo, AdminEmployeeRequestRepo>();
builder.Services.AddScoped<IAdminEmployeeRequestService, AdminEmployeeRequestService>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IManagerEmploymentContractService,
    WorkShiftManagement.BLL.Services.Manager.ManagerEmploymentContractService>();
builder.Services.AddSingleton<WorkShiftManagement.BLL.Interfaces.Manager.IContractPdfGenerator,
    WorkShiftManagement.BLL.Services.Manager.ContractPdfGenerator>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService,
    WorkShiftManagement.BLL.Services.Staff.StaffEmploymentContractService>();
builder.Services.AddScoped<IBranchRepo, BranchRepo>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ICategoryRepo, CategoryRepo>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductRepo, ProductRepo>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPublicHolidayRepo, PublicHolidayRepo>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<HolidayConfigurationRepo>();
builder.Services.AddScoped<HolidayPayRuleService>();
builder.Services.AddHostedService<HolidayRuleActivationWorker>();
builder.Services.AddHostedService<EmploymentContractActivationWorker>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Manager.IRequestLimitRuleRepo,
    WorkShiftManagement.DAL.Repositories.Manager.RequestLimitRuleRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IRequestLimitRuleService,
    WorkShiftManagement.BLL.Services.Manager.RequestLimitRuleService>();
builder.Services.AddHostedService<RequestLimitRuleActivationWorker>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Manager.IManagerDashboardRepo,
    WorkShiftManagement.DAL.Repositories.Manager.ManagerDashboardRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IManagerDashboardService,
    WorkShiftManagement.BLL.Services.Manager.ManagerDashboardService>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Accountant.IAccountantDashboardRepo,
    WorkShiftManagement.DAL.Repositories.Accountant.AccountantDashboardRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Accountant.IAccountantDashboardService,
    WorkShiftManagement.BLL.Services.Accountant.AccountantDashboardService>();
builder.Services.AddScoped<IManagerStaffRepo, ManagerStaffRepo>();
builder.Services.AddScoped<IManagerStaffService, ManagerStaffService>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Manager.IManagerEmployeeRequestRepo,
    WorkShiftManagement.DAL.Repositories.Manager.ManagerEmployeeRequestRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IManagerEmployeeRequestService,
    WorkShiftManagement.BLL.Services.Manager.ManagerEmployeeRequestService>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Staff.IStaffEmployeeRequestRepo,
    WorkShiftManagement.DAL.Repositories.Staff.StaffEmployeeRequestRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmployeeRequestService,
    WorkShiftManagement.BLL.Services.Staff.StaffEmployeeRequestService>();

builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Manager.IManagerTimesheetRepo,
    WorkShiftManagement.DAL.Repositories.Manager.ManagerTimesheetRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IManagerTimesheetService,
    WorkShiftManagement.BLL.Services.Manager.ManagerTimesheetService>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Staff.IStaffTimesheetRepo,
    WorkShiftManagement.DAL.Repositories.Staff.StaffTimesheetRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Staff.IStaffTimesheetService,
    WorkShiftManagement.BLL.Services.Staff.StaffTimesheetService>();

builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Staff.IStaffSalaryHistoryRepo,
    WorkShiftManagement.DAL.Repositories.Staff.StaffSalaryHistoryRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Staff.IStaffSalaryHistoryService,
    WorkShiftManagement.BLL.Services.Staff.StaffSalaryHistoryService>();

builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Upload.ICloudinaryService, WorkShiftManagement.BLL.Services.Upload.CloudinaryService>();

builder.Services.AddScoped<IShiftTemplateRepo, ShiftTemplateRepo>();
builder.Services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
builder.Services.AddScoped<IDashboardRepo, DashboardRepo>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationRepo, NotificationRepo>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPersonalNotificationRepo, PersonalNotificationRepo>();
builder.Services.AddScoped<IPersonalNotificationService, PersonalNotificationService>();
builder.Services.AddScoped<IBroadcastNotificationService, BroadcastNotificationService>();
builder.Services.AddScoped<IScheduleWeekRepo, ScheduleWeekRepo>();
builder.Services.AddScoped<IScheduleWeekService, ScheduleWeekService>();
builder.Services.AddScoped<IShiftRepo, ShiftRepo>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IShiftAssignmentRepo, ShiftAssignmentRepo>();
builder.Services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
builder.Services.AddHttpClient<INotificationEmailSender, ResendNotificationEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<IFaceRegistrationRepo, FaceRegistrationRepo>();
builder.Services.AddScoped<IFaceRegistrationService, FaceRegistrationService>();
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Staff.IAttendanceRepo, WorkShiftManagement.DAL.Repositories.Staff.AttendanceRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Staff.IAttendanceService, WorkShiftManagement.BLL.Services.Staff.AttendanceService>();
builder.Services.AddSingleton<IFaceBiometricService, FaceBiometricService>();
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
builder.Services.AddSingleton<IAdminRealtimePublisher, SignalRAdminRealtimePublisher>();

var app = builder.Build();

try
{
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IWorkingTimeRuleService>(
            "working-time-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            Cron.Minutely());

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IAttendanceRuleService>(
            "attendance-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            Cron.Minutely());

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IAttendancePenaltyRuleService>(
            "attendance-penalty-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            Cron.Minutely());
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "scheduled-contracts-activation",
            service => service.ActivateDueScheduledContractsAsync(CancellationToken.None),
            Cron.Minutely());
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "expired-contracts-worker",
            service => service.ExpireDueContractsAsync(CancellationToken.None),
            Cron.Minutely());
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "overdue-contracts-cancellation",
            service => service.CancelOverduePendingContractsAsync(CancellationToken.None),
            Cron.Minutely());

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Manager.IManagerTimesheetService>(
            "timesheet-deadline-autolock",
            service => service.AutoLockExpiredTimesheetsAsync(CancellationToken.None),
            Cron.Minutely());
}

catch (Exception ex)
{
    // A Hangfire distributed-lock timeout (e.g. Supabase pooler) must not kill API startup.
    app.Logger.LogError(ex, "Failed to register Hangfire recurring job 'working-time-rules-activation'.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard();
}

// app.UseHttpsRedirection(); // Commented out to prevent HTTP 307 redirect when behind ngrok/proxy
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<AdminRealtimeHub>("/hubs/admin-realtime");

app.Run();
