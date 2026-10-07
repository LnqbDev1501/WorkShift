using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Admin.UserAdmin
{
    public class SetPasswordViewModel
    {
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;

        [Required(ErrorMessage = "The password setup link is invalid or has expired.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a password.")]
        [MinLength(6, ErrorMessage = "The password must contain at least 6 characters.")]
        [MaxLength(16, ErrorMessage = "The password must not exceed 16 characters.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,16}$",
            ErrorMessage = "The password must contain uppercase and lowercase letters, a number, and a special character. Spaces are not allowed.")]
        [DataType(DataType.Password)]
        public string Password
        {
            get => _password;
            set => _password = value?.Trim() ?? string.Empty;
        }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Display(Name = "Confirm Password")]
        [Compare(
                nameof(Password),
                ErrorMessage = "The password confirmation does not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => _confirmPassword = value?.Trim() ?? string.Empty;
        }
    }
}
