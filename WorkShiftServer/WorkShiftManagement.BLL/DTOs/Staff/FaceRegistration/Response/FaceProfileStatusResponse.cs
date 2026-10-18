namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

public class FaceProfileStatusResponse
{
    public bool HasRegisteredFace { get; set; }
    public int? FaceProfileId { get; set; }
    public string? Status { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string? RegistrationType { get; set; }
    public bool CanRegisterOrUpdate { get; set; }
    public DateTime? NextAllowedRegistrationDate { get; set; }
    public string? Message { get; set; }
}
