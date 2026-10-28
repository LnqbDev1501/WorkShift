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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR browser transports send the bearer token as a query parameter.
                if (context.Request.Path.StartsWithSegments("/hubs/payroll-notifications")
                    && context.Request.Query.TryGetValue("access_token", out var token))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
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
// and Hangfire workers so background jobs cannot starve web requests or hit EMAXCONNSESSION.
var efConnBuilder = new Npgsql.NpgsqlConnectionStringBuilder(myCnn)
{
    MaxPoolSize = 8,
    MinPoolSize = 0,
    ConnectionIdleLifetime = 5,
    ConnectionPruningInterval = 2,
    Timeout = 15,
    CommandTimeout = 30
};
var pooledConnectionString = efConnBuilder.ToString();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(pooledConnectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

var hangfireRaw = builder.Configuration.GetConnectionString("Hangfire") ?? myCnn;
var hangfireConnBuilder = new Npgsql.NpgsqlConnectionStringBuilder(hangfireRaw)
{
    MaxPoolSize = 3,
    MinPoolSize = 0,
    ConnectionIdleLifetime = 5,
    ConnectionPruningInterval = 2,
    Timeout = 30,
    CommandTimeout = 30
};
var hangfireConnection = hangfireConnBuilder.ToString();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection), new PostgreSqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1)
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
});

builder.Services.AddScoped<IPayrollApprovalRepo, PayrollApprovalRepo>();
builder.Services.AddScoped<IPayrollApprovalService, PayrollApprovalService>();
builder.Services.AddScoped<IPayrollChangeTransaction>(sp => sp.GetRequiredService<HolidayConfigurationRepo>());
builder.Services.AddScoped<ISalaryHistoryRepo, SalaryHistoryRepo>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<IPayrollPeriodRepo, PayrollPeriodRepo>();
builder.Services.AddScoped<IPayrollPeriodService, PayrollPeriodService>();
builder.Services.AddScoped<IPayrollCalculationRepo, PayrollCalculationRepo>();
builder.Services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
builder.Services.AddScoped<IPayrollRevisionRepo, PayrollRevisionRepo>();
builder.Services.AddScoped<IPayrollRevisionService, PayrollRevisionService>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IPayslipRepo, PayslipRepo>();
builder.Services.AddScoped<IPayslipService, PayslipService>();
builder.Services.AddScoped<IPayrollFeedbackRepo, PayrollFeedbackRepo>();
builder.Services.AddScoped<IPayrollFeedbackService, PayrollFeedbackService>();
builder.Services.AddScoped<PayslipPublicationJob>();
builder.Services.AddScoped<IPayrollSystemEventRepo, PayrollSystemEventRepo>();
builder.Services.AddScoped<IPayrollSystemNotificationService, PayrollSystemNotificationService>();
builder.Services.AddScoped<PayrollSystemNotificationJob>();
builder.Services.AddScoped<IPayrollSettlementRepo, PayrollSettlementRepo>();
builder.Services.AddScoped<IPayrollSettlementService, PayrollSettlementService>();
builder.Services.AddScoped<IPayrollClosureNotifier, HangfirePayrollClosureNotifier>();
builder.Services.AddScoped<PayrollClosureNotificationJob>();

builder.Services.AddHttpClient<AuthService>(client =>
{
    var serviceRoleKey = builder.Configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Missing Supabase:ServiceRoleKey configuration.");

    client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
}).AddTypedClient<IAuthService>((client, sp) =>
{
    var setPasswordUrl = builder.Configuration["AdminUi:SetPasswordUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:SetPasswordUrl configuration.");
    var resetPasswordUrl = builder.Configuration["AdminUi:ResetPasswordUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:ResetPasswordUrl configuration.");
    var staffSetupUrl = builder.Configuration["AdminUi:StaffSetupUrl"]
        ?? throw new InvalidOperationException("Missing AdminUi:StaffSetupUrl configuration.");
    var context = sp.GetRequiredService<AppDbContext>();
    return new AuthService(client, context, setPasswordUrl, resetPasswordUrl, staffSetupUrl);
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
builder.Services.AddScoped<IOrderRepo, OrderRepo>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Accountant.IBranchSalesReportService,
    WorkShiftManagement.BLL.Services.Accountant.BranchSalesReportService>();
builder.Services.AddScoped<IPaymentRepo, PaymentRepo>();
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
builder.Services.AddScoped<IEmployeeAdjustmentRepo, EmployeeAdjustmentRepo>();
builder.Services.AddScoped<IEmployeeAdjustmentService, EmployeeAdjustmentService>();
builder.Services.AddScoped<ISalaryChangeRepo, SalaryChangeRepo>();
builder.Services.AddScoped<ISalaryChangeService, SalaryChangeService>();
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
builder.Services.AddScoped<WorkShiftManagement.DAL.Interfaces.Manager.IManagerAttendanceRepo,
    WorkShiftManagement.DAL.Repositories.Manager.ManagerAttendanceRepo>();
builder.Services.AddScoped<WorkShiftManagement.BLL.Interfaces.Manager.IManagerAttendanceService,
    WorkShiftManagement.BLL.Services.Manager.ManagerAttendanceService>();
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
builder.Services.AddScoped<IStaffShiftRepo, StaffShiftRepo>();
builder.Services.AddScoped<IStaffShiftService, StaffShiftService>();
builder.Services.AddScoped<IStaffShiftSwapRepo, StaffShiftSwapRepo>();
builder.Services.AddScoped<IStaffShiftSwapService, StaffShiftSwapService>();
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
        .AddOrUpdate<IStaffShiftSwapService>("shift-swap-expiry",
            service => service.ExpireDueAsync(CancellationToken.None), Cron.Minutely());
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to register Hangfire recurring job 'shift-swap-expiry'.");
}

// Register independently: another recurring job's lock timeout must not skip salary activation.
try
{
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<ISalaryChangeService>("salary-change-activation",
            service => service.ActivateDueAsync(CancellationToken.None), Cron.Minutely());
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to register Hangfire recurring job 'salary-change-activation'.");
}

foreach (var registerPayrollJob in new Action<IRecurringJobManager>[]
{
    jobs => jobs.AddOrUpdate<IPayrollFeedbackService>("payroll-preview-expiry", service => service.ExpireDueAsync(CancellationToken.None), "*/5 * * * *"),
    jobs => jobs.AddOrUpdate<PayrollSystemNotificationJob>("payroll-system-notifications", job => job.ExecuteAsync(CancellationToken.None), "*/5 * * * *"),
    jobs => { jobs.RemoveIfExists("initial-payslip-publication"); jobs.AddOrUpdate<PayslipPublicationJob>("approved-payslip-recovery", job => job.ExecuteAsync(CancellationToken.None), "*/5 * * * *"); }
})
{
    try { registerPayrollJob(app.Services.GetRequiredService<IRecurringJobManager>()); }
    catch (Exception ex) { app.Logger.LogError(ex, "Failed to register a payroll workflow recurring job."); }
}

try
{
    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<PayrollClosureNotificationJob>(
            "payroll-closure-notification-recovery",
            job => job.SendPendingAsync(CancellationToken.None),
            "*/5 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IWorkingTimeRuleService>(
            "working-time-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            "*/30 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IAttendanceRuleService>(
            "attendance-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            "*/30 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<IAttendancePenaltyRuleService>(
            "attendance-penalty-rules-activation",
            service => service.ActivateDueAsync(CancellationToken.None),
            "*/30 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "scheduled-contracts-activation",
            service => service.ActivateDueScheduledContractsAsync(CancellationToken.None),
            "*/15 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "expired-contracts-worker",
            service => service.ExpireDueContractsAsync(CancellationToken.None),
            "*/15 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmploymentContractService>(
            "overdue-contracts-cancellation",
            service => service.CancelOverduePendingContractsAsync(CancellationToken.None),
            "*/15 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Manager.IManagerTimesheetService>(
            "timesheet-deadline-autolock",
            service => service.AutoLockExpiredTimesheetsAsync(CancellationToken.None),
            "*/5 * * * *");

    app.Services.GetRequiredService<IRecurringJobManager>()
        .AddOrUpdate<WorkShiftManagement.BLL.Interfaces.Staff.IStaffEmployeeRequestService>(
            "overdue-requests-cancellation",
            service => service.CancelOverduePendingRequestsAsync(CancellationToken.None),
            "*/5 * * * *");
}

catch (Exception ex)
{
    // A Hangfire distributed-lock timeout (e.g. Supabase pooler) must not kill API startup.
    app.Logger.LogError(ex, "Failed to register Hangfire recurring jobs.");
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
app.MapHub<PayrollNotificationHub>("/hubs/payroll-notifications");

try
{
    using var cleanupScope = app.Services.CreateScope();
    var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var notifsToClean = await dbContext.Notifications
        .Where(n => n.Content.Contains("NOT_ACCEPTED_BY_START_DATE"))
        .ToListAsync();
    if (notifsToClean.Count > 0)
    {
        foreach (var n in notifsToClean)
        {
            n.Content = n.Content
                .Replace("Lý do: NOT_ACCEPTED_BY_START_DATE (quá ngày bắt đầu", "Lý do: Quá ngày bắt đầu")
                .Replace("Lý do: NOT_ACCEPTED_BY_START_DATE (Quá ngày bắt đầu", "Lý do: Quá ngày bắt đầu")
                .Replace("chưa xác nhận chấp thuận).", "chưa xác nhận chấp thuận.")
                .Replace("chưa xác nhận chấp thuận)", "chưa xác nhận chấp thuận")
                .Replace("NOT_ACCEPTED_BY_START_DATE", "Quá ngày bắt đầu mà nhân viên chưa xác nhận chấp thuận");
        }
        await dbContext.SaveChangesAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to clean up legacy notification messages.");
}

app.Run();
