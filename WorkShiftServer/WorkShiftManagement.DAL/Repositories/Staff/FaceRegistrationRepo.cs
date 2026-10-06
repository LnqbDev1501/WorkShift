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

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<EmploymentContract?> GetActiveContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        return await _context.EmploymentContracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Status == "ACTIVE" &&
                c.StartDate <= today &&
                (!c.EndDate.HasValue || c.EndDate.Value >= today),
                cancellationToken);
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
