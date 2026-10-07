using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;

namespace WorkShiftManagement.BLL.Interfaces.Staff;

public interface IFaceRegistrationService
{
    Task<FaceProfileStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FaceRegistrationSessionResponse> CreateSessionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CancelSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<FaceRegistrationVerifyResponse> VerifyAndRegisterAsync(Guid userId, VerifyFaceRegistrationRequest request, CancellationToken cancellationToken = default);
}
