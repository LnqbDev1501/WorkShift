namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class CreateScheduleWeekRequest
{
    public DateOnly WeekStartDate { get; set; }
    public DateTimeOffset RegistrationDeadline { get; set; }
}
