namespace WorkShiftManagement.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<Guid> CreateUserAndSendInviteAsync(string email);
    Task DeleteUserAsync(Guid userId);
}
