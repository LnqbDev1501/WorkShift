using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
