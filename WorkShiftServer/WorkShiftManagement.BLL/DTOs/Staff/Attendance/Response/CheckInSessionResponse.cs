using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Response;

public sealed class CheckInSessionResponse
{
    public Guid SessionId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string ActionType { get; set; } = "CHECK_IN";
    public List<FaceChallengeItemDto> Challenges { get; set; } = new();
}
