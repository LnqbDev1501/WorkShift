using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;

public class CancelFaceRegistrationSessionRequest
{
    [Required(ErrorMessage = "SessionId is required.")]
    public Guid SessionId { get; set; }
}
