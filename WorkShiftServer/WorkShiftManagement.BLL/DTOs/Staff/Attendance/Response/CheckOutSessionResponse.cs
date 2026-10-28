using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class CheckOutSessionResponse
{
    public Guid SessionId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string ActionType { get; set; } = "CHECK_OUT";
    public List<FaceChallengeItemDto> Challenges { get; set; } = new();
}
