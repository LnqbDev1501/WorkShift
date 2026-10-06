using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public class StaffEligibleShiftResponse
{
    public int AssignmentId { get; set; }

    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = null!;

    public string? ShiftTemplateName { get; set; }

    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public DateTime ScheduledStartAt { get; set; }

    public DateTime ScheduledEndAt { get; set; }
}
