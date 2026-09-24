using WorkShiftManagement.DAL.Repositories.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public static class HolidayWorkflowPolicy
{
    public static void RequireFuture(DateOnly date, DateOnly today)
    {
        if (date <= today)
            throw HolidayConfigurationRepo.Conflict("Choose a date after today (Vietnam time).", "FUTURE_DATE_REQUIRED");
    }

    public static void RequireEditable(bool active, bool usedInPayroll)
    {
        if (!active || usedInPayroll)
            throw HolidayConfigurationRepo.Conflict("Inactive holidays and holidays used in any payroll are read-only.", "HOLIDAY_READ_ONLY");
    }
}
