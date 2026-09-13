using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; set; }
}
