namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

public class FaceRegistrationVerifyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int FaceProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? FaceQualityScore { get; set; }
    public decimal LivenessScore { get; set; }
    public DateTime RegisteredAt { get; set; }
}
