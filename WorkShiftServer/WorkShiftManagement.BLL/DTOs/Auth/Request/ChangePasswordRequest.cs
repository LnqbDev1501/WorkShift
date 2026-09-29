using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public class ChangePasswordRequest
{
    private string _password = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must contain at least 6 characters.")]
    [MaxLength(16, ErrorMessage = "Password must not exceed 16 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
        ErrorMessage = "Password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
    public string Password
    {
        get => _password;
        set => _password = value?.Trim() ?? string.Empty;
    }
}
