namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public sealed class StaffShiftSwapCandidateResponse
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = "";
    public ShiftSwapAssignmentResponse Assignment { get; set; } = new();
}
