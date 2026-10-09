namespace WorkShiftManagement.BLL.DTOs.Manager.Request;

public sealed class UpdateScheduleWeekRequest
{
    public DateOnly WeekStartDate { get; set; }
    public DateTimeOffset RegistrationDeadline { get; set; }
}
