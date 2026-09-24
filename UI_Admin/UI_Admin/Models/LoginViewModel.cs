using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class LoginViewModel
{
    private string _email = string.Empty;
    private string _password = string.Empty;

    [Required, EmailAddress]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    [Required, DataType(DataType.Password)]
    public string Password
    {
        get => _password;
        set => _password = value?.Trim() ?? string.Empty;
    }

    public string? ReturnUrl { get; set; }
}
