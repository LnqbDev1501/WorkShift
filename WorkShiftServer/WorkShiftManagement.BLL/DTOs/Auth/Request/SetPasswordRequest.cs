using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public class SetPasswordRequest
{
    [Required(ErrorMessage = "Access token is required.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must contain at least 6 characters.")]
    [MaxLength(16, ErrorMessage = "Password must not exceed 16 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
        ErrorMessage = "Password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
    public string Password { get; set; } = string.Empty;
}
