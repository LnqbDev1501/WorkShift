using UI_Admin.Services;
using UI_Admin.Services.Accountant;
using UI_Admin.Services.Admin;
using UI_Admin.Services.Manager;
using UI_Admin.Services.Staff;

namespace UI_Admin.Extensions;

public static class ApiClientServiceExtensions
{
    public static IServiceCollection AddWorkShiftApiClients(this IServiceCollection services)
    {
        services.AddTransient<ApiBearerTokenHandler>();

        // Shared
        services.AddHttpClient<AuthApiService>();
        AddAuthenticatedApiClient<CategoryApiService>(services);
        AddAuthenticatedApiClient<DashboardApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<EmployeeAdjustmentApiService>(services, TimeSpan.FromSeconds(15));
        services.AddHttpClient<IVietMapService, VietMapService>(client => client.Timeout = TimeSpan.FromSeconds(10));
        AddAuthenticatedApiClient<NotificationApiService>(services);
        AddAuthenticatedApiClient<PayrollCalculationApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<PayrollDocumentsApiService>(services, TimeSpan.FromSeconds(60));
        AddAuthenticatedApiClient<PayrollPeriodReportApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<ProductApiService>(services);
        AddAuthenticatedApiClient<SalaryChangeApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<ShiftTemplateApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<WorkingTimeRuleApiService>(services, TimeSpan.FromSeconds(30));

        // Admin
        AddAuthenticatedApiClient<AttendancePenaltyRuleApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<AttendanceRuleApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<BranchApiService>(services);
        AddAuthenticatedApiClient<EmployeeRequestApiService>(services, TimeSpan.FromSeconds(20));
        AddAuthenticatedApiClient<EmployeeTypeApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<EmploymentContractApiService>(services);
        AddAuthenticatedApiClient<HolidayApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<PayrollApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<RegionApiService>(services);
        AddAuthenticatedApiClient<RegionalMinimumWageApiService>(services);
        AddAuthenticatedApiClient<UserApiService>(services);

        // Manager
        AddAuthenticatedApiClient<ManagerAttendanceApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerContractApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<ManagerDashboardApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerEmployeeRequestApiService>(services, TimeSpan.FromSeconds(20));
        AddAuthenticatedApiClient<ManagerOrderApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<ManagerProductApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<ManagerRequestLimitRuleApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerScheduleApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerShiftApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerStaffApiService>(services);
        AddAuthenticatedApiClient<ManagerTimesheetAdjustmentApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<ManagerTimesheetApiService>(services, TimeSpan.FromSeconds(60));

        // Accountant
        AddAuthenticatedApiClient<AccountantDashboardApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<AccountantOrderApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<AccountantProductApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<PayrollCalculationActionApiService>(services, TimeSpan.FromMinutes(2));
        AddAuthenticatedApiClient<PayrollPeriodApiService>(services, TimeSpan.FromSeconds(15));
        AddAuthenticatedApiClient<SalaryHistoryApiService>(services, TimeSpan.FromSeconds(15));

        // Staff
        AddAuthenticatedApiClient<StaffContractApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<StaffEmployeeRequestApiService>(services, TimeSpan.FromSeconds(20));
        AddAuthenticatedApiClient<StaffOrderApiService>(services, TimeSpan.FromSeconds(30));
        AddAuthenticatedApiClient<StaffShiftApiService>(services, TimeSpan.FromSeconds(30));

        return services;
    }

    private static void AddAuthenticatedApiClient<TClient>(IServiceCollection services, TimeSpan? timeout = null)
        where TClient : class
    {
        services.AddHttpClient<TClient>(client =>
        {
            if (timeout.HasValue)
                client.Timeout = timeout.Value;
        }).AddHttpMessageHandler<ApiBearerTokenHandler>();
    }
}
