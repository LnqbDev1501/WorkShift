using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public class FaceRegistrationRepo : IFaceRegistrationRepo
{
    private readonly AppDbContext _context;

    public FaceRegistrationRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FaceProfile?> GetActiveFaceProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.FaceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == "ACTIVE", cancellationToken);
    }

    public async Task<FaceVerificationSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.FaceVerificationSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<FaceVerificationSession?> GetLatestSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.FaceVerificationSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateSessionAsync(FaceVerificationSession session, CancellationToken cancellationToken = default)
    {
        await _context.FaceVerificationSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateFaceProfileAsync(FaceProfile faceProfile, CancellationToken cancellationToken = default)
    {
        await _context.FaceProfiles.AddAsync(faceProfile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
