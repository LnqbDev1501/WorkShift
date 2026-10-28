using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Notification;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.BLL.Interfaces.Upload;
using WorkShiftManagement.BLL.Services.Accountant;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.BLL.Services.Auth;
using WorkShiftManagement.BLL.Services.Manager;
using WorkShiftManagement.BLL.Services.Notification;
using WorkShiftManagement.BLL.Services.Staff;
using WorkShiftManagement.BLL.Services.Upload;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Interfaces.Notification;
using WorkShiftManagement.DAL.Interfaces.Staff;
using WorkShiftManagement.DAL.Repositories.Accountant;
using WorkShiftManagement.DAL.Repositories.Admin;
using WorkShiftManagement.DAL.Repositories.Manager;
using WorkShiftManagement.DAL.Repositories.Notification;
using WorkShiftManagement.DAL.Repositories.Staff;
using WorkShiftServer.API.Services;
using WorkShiftServer.API.Services.Realtime;

namespace WorkShiftServer.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Missing Supabase:Url configuration.");

        AddSharedServices(services);
        AddAuthenticationServices(services, configuration, supabaseUrl);
        AddAdminServices(services);
        AddManagerServices(services);
        AddStaffServices(services);
        AddPayrollServices(services);
        AddNotificationServices(services);

        return services;
    }

    private static void AddSharedServices(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<ICloudinaryService, CloudinaryService>();
    }

    private static void AddAuthenticationServices(IServiceCollection services, IConfiguration configuration, string supabaseUrl)
    {
        services.AddHttpClient<AuthService>(client =>
        {
            var serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Missing Supabase:ServiceRoleKey configuration.");

            client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/");
            client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
        }).AddTypedClient<IAuthService>((client, sp) =>
        {
            var setPasswordUrl = configuration["AdminUi:SetPasswordUrl"]
                ?? throw new InvalidOperationException("Missing AdminUi:SetPasswordUrl configuration.");
            var resetPasswordUrl = configuration["AdminUi:ResetPasswordUrl"]
                ?? throw new InvalidOperationException("Missing AdminUi:ResetPasswordUrl configuration.");
            var staffSetupUrl = configuration["AdminUi:StaffSetupUrl"]
                ?? throw new InvalidOperationException("Missing AdminUi:StaffSetupUrl configuration.");
            var context = sp.GetRequiredService<AppDbContext>();
            return new AuthService(client, context, setPasswordUrl, resetPasswordUrl, staffSetupUrl);
        });
        services.AddScoped<IProfileService, ProfileService>();
        services.AddSingleton<ProfileEmailChangeTokenService>();
        services.AddSingleton<StaffSetupTokenService>();
        services.AddHttpClient<IAccountSetupEmailSender, ResendAccountSetupEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
        services.AddHttpClient<IStaffSetupEmailSender, ResendStaffSetupEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
        services.AddHttpClient<IProfileEmailChangeSender, ResendProfileEmailChangeSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
    }

    private static void AddAdminServices(IServiceCollection services)
    {
        services.AddScoped<IPayrollApprovalRepo, PayrollApprovalRepo>();
        services.AddScoped<IPayrollApprovalService, PayrollApprovalService>();
        services.AddScoped<IPayrollChangeTransaction>(sp => sp.GetRequiredService<HolidayConfigurationRepo>());
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorkingTimeRuleRepo, WorkingTimeRuleRepo>();
        services.AddScoped<IWorkingTimeRuleService, WorkingTimeRuleService>();
        services.AddScoped<IAttendanceRuleRepo, AttendanceRuleRepo>();
        services.AddScoped<IAttendanceRuleService, AttendanceRuleService>();
        services.AddScoped<IAttendancePenaltyRuleRepo, AttendancePenaltyRuleRepo>();
        services.AddScoped<IAttendancePenaltyRuleService, AttendancePenaltyRuleService>();
        services.AddScoped<IRegionRepo, RegionRepo>();
        services.AddScoped<IEmployeeTypeRepo, EmployeeTypeRepo>();
        services.AddScoped<IEmployeeTypeService, EmployeeTypeService>();
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IRegionalMinimumWageRepo, RegionalMinimumWageRepo>();
        services.AddScoped<IRegionalMinimumWageService, RegionalMinimumWageService>();
        services.AddHostedService<RegionalMinimumWageActivationWorker>();
        services.AddScoped<IEmploymentContractRepo, EmploymentContractRepo>();
        services.AddScoped<IAdminEmploymentContractService, AdminEmploymentContractService>();
        services.AddScoped<IAdminEmployeeRequestRepo, AdminEmployeeRequestRepo>();
        services.AddScoped<IAdminEmployeeRequestService, AdminEmployeeRequestService>();
        services.AddScoped<IBranchRepo, BranchRepo>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ICategoryRepo, CategoryRepo>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductRepo, ProductRepo>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderRepo, OrderRepo>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentRepo, PaymentRepo>();
        services.AddScoped<IPublicHolidayRepo, PublicHolidayRepo>();
        services.AddScoped<IPublicHolidayService, PublicHolidayService>();
        services.AddScoped<HolidayConfigurationRepo>();
        services.AddScoped<HolidayPayRuleService>();
        services.AddHostedService<HolidayRuleActivationWorker>();
        services.AddHostedService<EmploymentContractActivationWorker>();
        services.AddScoped<IShiftTemplateRepo, ShiftTemplateRepo>();
        services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
        services.AddScoped<IDashboardRepo, DashboardRepo>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationRepo, NotificationRepo>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHttpClient<INotificationEmailSender, ResendNotificationEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
        services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
    }

    private static void AddManagerServices(IServiceCollection services)
    {
        services.AddScoped<IManagerEmploymentContractService, ManagerEmploymentContractService>();
        services.AddSingleton<IContractPdfGenerator, ContractPdfGenerator>();
        services.AddScoped<IRequestLimitRuleRepo, RequestLimitRuleRepo>();
        services.AddScoped<IRequestLimitRuleService, RequestLimitRuleService>();
        services.AddHostedService<RequestLimitRuleActivationWorker>();
        services.AddScoped<IManagerDashboardRepo, ManagerDashboardRepo>();
        services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
        services.AddScoped<IManagerStaffRepo, ManagerStaffRepo>();
        services.AddScoped<IManagerStaffService, ManagerStaffService>();
        services.AddScoped<IEmployeeAdjustmentRepo, EmployeeAdjustmentRepo>();
        services.AddScoped<IEmployeeAdjustmentService, EmployeeAdjustmentService>();
        services.AddScoped<ISalaryChangeRepo, SalaryChangeRepo>();
        services.AddScoped<ISalaryChangeService, SalaryChangeService>();
        services.AddScoped<IManagerEmployeeRequestRepo, ManagerEmployeeRequestRepo>();
        services.AddScoped<IManagerEmployeeRequestService, ManagerEmployeeRequestService>();
        services.AddScoped<IManagerTimesheetRepo, ManagerTimesheetRepo>();
        services.AddScoped<IManagerTimesheetService, ManagerTimesheetService>();
        services.AddScoped<IManagerAttendanceRepo, ManagerAttendanceRepo>();
        services.AddScoped<IManagerAttendanceService, ManagerAttendanceService>();
        services.AddScoped<IScheduleWeekRepo, ScheduleWeekRepo>();
        services.AddScoped<IScheduleWeekService, ScheduleWeekService>();
        services.AddScoped<IShiftRepo, ShiftRepo>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IShiftAssignmentRepo, ShiftAssignmentRepo>();
        services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
    }

    private static void AddStaffServices(IServiceCollection services)
    {
        services.AddScoped<IStaffEmploymentContractService, StaffEmploymentContractService>();
        services.AddScoped<IStaffEmployeeRequestRepo, StaffEmployeeRequestRepo>();
        services.AddScoped<IStaffEmployeeRequestService, StaffEmployeeRequestService>();
        services.AddScoped<IStaffTimesheetRepo, StaffTimesheetRepo>();
        services.AddScoped<IStaffTimesheetService, StaffTimesheetService>();
        services.AddScoped<IStaffSalaryHistoryRepo, StaffSalaryHistoryRepo>();
        services.AddScoped<IStaffSalaryHistoryService, StaffSalaryHistoryService>();
        services.AddScoped<IStaffShiftRepo, StaffShiftRepo>();
        services.AddScoped<IStaffShiftService, StaffShiftService>();
        services.AddScoped<IStaffShiftSwapRepo, StaffShiftSwapRepo>();
        services.AddScoped<IStaffShiftSwapService, StaffShiftSwapService>();
        services.AddScoped<IFaceRegistrationRepo, FaceRegistrationRepo>();
        services.AddScoped<IFaceRegistrationService, FaceRegistrationService>();
        services.AddScoped<IAttendanceRepo, AttendanceRepo>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddSingleton<IFaceBiometricService, FaceBiometricService>();
    }

    private static void AddPayrollServices(IServiceCollection services)
    {
        services.AddScoped<ISalaryHistoryRepo, SalaryHistoryRepo>();
        services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
        services.AddScoped<IPayrollPeriodRepo, PayrollPeriodRepo>();
        services.AddScoped<IPayrollPeriodService, PayrollPeriodService>();
        services.AddScoped<IPayrollCalculationRepo, PayrollCalculationRepo>();
        services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
        services.AddScoped<IPayrollRevisionRepo, PayrollRevisionRepo>();
        services.AddScoped<IPayrollRevisionService, PayrollRevisionService>();
        services.AddScoped<IPayslipRepo, PayslipRepo>();
        services.AddScoped<IPayslipService, PayslipService>();
        services.AddScoped<IPayrollFeedbackRepo, PayrollFeedbackRepo>();
        services.AddScoped<IPayrollFeedbackService, PayrollFeedbackService>();
        services.AddScoped<PayslipPublicationJob>();
        services.AddScoped<IPayrollSystemEventRepo, PayrollSystemEventRepo>();
        services.AddScoped<IPayrollSystemNotificationService, PayrollSystemNotificationService>();
        services.AddScoped<PayrollSystemNotificationJob>();
        services.AddScoped<IPayrollSettlementRepo, PayrollSettlementRepo>();
        services.AddScoped<IPayrollSettlementService, PayrollSettlementService>();
        services.AddScoped<IPayrollClosureNotifier, HangfirePayrollClosureNotifier>();
        services.AddScoped<PayrollClosureNotificationJob>();
        services.AddScoped<IBranchSalesReportService, BranchSalesReportService>();
        services.AddScoped<IAccountantDashboardRepo, AccountantDashboardRepo>();
        services.AddScoped<IAccountantDashboardService, AccountantDashboardService>();
    }

    private static void AddNotificationServices(IServiceCollection services)
    {
        services.AddScoped<IPersonalNotificationRepo, PersonalNotificationRepo>();
        services.AddScoped<IPersonalNotificationService, PersonalNotificationService>();
        services.AddScoped<IBroadcastNotificationService, BroadcastNotificationService>();
        services.AddSingleton<IAdminRealtimePublisher, SignalRAdminRealtimePublisher>();
    }
}
