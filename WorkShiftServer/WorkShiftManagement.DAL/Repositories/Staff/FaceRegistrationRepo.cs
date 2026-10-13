using System.Data;
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

    public async Task<EmploymentContract?> GetLatestContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmploymentContracts
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task CreateFaceProfileAsync(FaceProfile faceProfile, float[] embedding, CancellationToken cancellationToken = default)
    {
        var qualityScore = faceProfile.FaceQualityScore ?? 0.9850m;
        var livenessScore = faceProfile.LivenessScore;

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        string embeddingString = "[" + string.Join(",", embedding.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO public.face_profiles (
                user_id,
                registration_session_id,
                face_embedding,
                embedding_version,
                registration_type,
                face_quality_score,
                liveness_score,
                status,
                registered_at
            ) VALUES (
                @userId,
                @sessionId,
                @embeddingStr::vector,
                @embeddingVersion,
                @regType,
                @qualityScore,
                @livenessScore,
                @status,
                @registeredAt
            ) RETURNING face_profile_id;";

        void AddParam(string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }

        AddParam("@userId", faceProfile.UserId);
        AddParam("@sessionId", faceProfile.RegistrationSessionId);
        AddParam("@embeddingStr", embeddingString);
        AddParam("@embeddingVersion", faceProfile.EmbeddingVersion ?? "ArcFace-512");
        AddParam("@regType", faceProfile.RegistrationType ?? "INITIAL");
        AddParam("@qualityScore", qualityScore);
        AddParam("@livenessScore", livenessScore);
        AddParam("@status", faceProfile.Status ?? "ACTIVE");
        AddParam("@registeredAt", faceProfile.RegisteredAt);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        if (scalar != null && int.TryParse(scalar.ToString(), out var profileId))
        {
            faceProfile.FaceProfileId = profileId;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
