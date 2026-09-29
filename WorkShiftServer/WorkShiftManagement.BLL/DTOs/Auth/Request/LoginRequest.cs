using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Auth.Request;

public sealed class LoginRequest
{
    private string _email = string.Empty;
    private string _password = string.Empty;

    [Required, EmailAddress]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string Password
    {
        get => _password;
        set => _password = value?.Trim() ?? string.Empty;
    }
}
