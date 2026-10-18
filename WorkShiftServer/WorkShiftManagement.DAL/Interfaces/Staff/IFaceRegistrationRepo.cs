using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IFaceRegistrationRepo
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmploymentContract?> GetActiveContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmploymentContract?> GetLatestContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FaceProfile?> GetActiveFaceProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FaceVerificationSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<FaceVerificationSession?> GetLatestSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(FaceVerificationSession session, CancellationToken cancellationToken = default);
    Task CreateFaceProfileAsync(FaceProfile faceProfile, float[] embedding, CancellationToken cancellationToken = default);
    Task<(bool IsDuplicate, string? MatchedUserName, float Similarity)> FindDuplicateFaceProfileAsync(Guid currentUserId, float[] embedding, float threshold = 0.60f, CancellationToken cancellationToken = default);
    Task RevokeFaceProfileAsync(int faceProfileId, DateTime revokedAt, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
