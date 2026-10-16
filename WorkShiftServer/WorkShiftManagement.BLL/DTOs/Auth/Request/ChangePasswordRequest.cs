using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public class ChangePasswordRequest
{
    private string _currentPassword = string.Empty;
    private string _password = string.Empty;

    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword
    {
        get => _currentPassword;
        set => _currentPassword = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "The password must contain at least 6 characters.")]
    [MaxLength(16, ErrorMessage = "The password must not exceed 16 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
        ErrorMessage = "The password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
    public string Password
    {
        get => _password;
        set => _password = value?.Trim() ?? string.Empty;
    }
}
