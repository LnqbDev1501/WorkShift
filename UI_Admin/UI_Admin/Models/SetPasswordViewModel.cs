using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "The password setup link is invalid or has expired.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a password.")]
        [MinLength(6, ErrorMessage = "The password must contain at least 6 characters.")]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z0-9\s])\S{6,}$",
            ErrorMessage = "The password must contain a letter, a number, at least one uppercase letter, and one special character.")]
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
