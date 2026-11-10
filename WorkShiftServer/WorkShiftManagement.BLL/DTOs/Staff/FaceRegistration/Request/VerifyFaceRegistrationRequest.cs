using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;

public class VerifyFaceRegistrationRequest
{
    [Required(ErrorMessage = "SessionId is required.")]
    public Guid SessionId { get; set; }

    [Required(ErrorMessage = "FaceImage file is required.")]
    public IFormFile FaceImage { get; set; } = null!;

    /// <summary>
    /// Liveness score computed by Google ML Kit on the client (0.0 – 1.0).
    /// Optional: defaults to 1.0 if not provided (web/test mode).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "LivenessScore must be between 0.0 and 1.0.")]
    public decimal? LivenessScore { get; set; }

    /// <summary>
    /// Indicates the request originates from Flutter Web test mode (only honored in Development).
    /// </summary>
    public bool IsWebTestMode { get; set; } = false;
}
