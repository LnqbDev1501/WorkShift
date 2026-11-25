using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class ForgotPasswordViewModel
{
    private string _email = string.Empty;

    [Required, EmailAddress]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    public bool Submitted { get; set; }
}
