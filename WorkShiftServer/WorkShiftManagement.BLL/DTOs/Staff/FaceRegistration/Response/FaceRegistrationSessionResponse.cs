namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

public class FaceRegistrationSessionResponse
{
    public Guid SessionId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public List<FaceChallengeItemDto> Challenges { get; set; } = new();
}
