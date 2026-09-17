using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "The password setup link is invalid or has expired.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a password.")]
        [MinLength(6, ErrorMessage = "The password must contain at least 6 characters.")]
        [MaxLength(16, ErrorMessage = "The password must not exceed 16 characters.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
            ErrorMessage = "The password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Display(Name = "Confirm Password")]
    [Compare(
            nameof(Password),
            ErrorMessage = "The password confirmation does not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
