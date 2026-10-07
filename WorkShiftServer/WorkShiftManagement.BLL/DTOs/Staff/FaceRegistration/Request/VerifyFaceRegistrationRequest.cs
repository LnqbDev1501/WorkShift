using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;

public class VerifyFaceRegistrationRequest
{
    [Required(ErrorMessage = "SessionId is required.")]
    public Guid SessionId { get; set; }

    [Required(ErrorMessage = "FaceImage file is required.")]
    public IFormFile FaceImage { get; set; } = null!;
}
