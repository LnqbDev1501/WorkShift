using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public class ChangePasswordViewModel
{
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    [Required(ErrorMessage = "Please enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword
    {
        get => _currentPassword;
        set => _currentPassword = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Please enter a new password.")]
    [MinLength(6, ErrorMessage = "The password must contain at least 6 characters.")]
    [MaxLength(16, ErrorMessage = "The password must not exceed 16 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
        ErrorMessage = "The password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword
    {
        get => _newPassword;
        set => _newPassword = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Display(Name = "Confirm New Password")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "The password confirmation does not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => _confirmPassword = value?.Trim() ?? string.Empty;
    }
}