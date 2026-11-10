using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
        var currentTransaction = _context.Database.CurrentTransaction;
        bool openedHere = currentTransaction == null && connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        string embeddingString = "[" + string.Join(",", embedding.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        try
        {
            await using var command = connection.CreateCommand();
            if (currentTransaction != null)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

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
        finally
        {
            if (openedHere)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task<(bool IsDuplicate, string? MatchedUserName, float Similarity)> FindDuplicateFaceProfileAsync(
        Guid currentUserId,
        float[] embedding,
        float threshold = 0.60f,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var currentTransaction = _context.Database.CurrentTransaction;
        bool openedHere = currentTransaction == null && connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        string embeddingString = "[" + string.Join(",", embedding.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        try
        {
            await using var command = connection.CreateCommand();
            if (currentTransaction != null)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            command.CommandText = @"
            SELECT fp.face_profile_id, fp.user_id, u.full_name, (1 - (fp.face_embedding <=> @embeddingStr::vector)) AS similarity
            FROM public.face_profiles fp
            JOIN public.users u ON u.user_id = fp.user_id
            WHERE fp.status = 'ACTIVE'
              AND fp.user_id != @userId
              AND fp.face_embedding IS NOT NULL
              AND u.deleted_at IS NULL
              AND u.status = 'ACTIVE'
            ORDER BY fp.face_embedding <=> @embeddingStr::vector ASC
            LIMIT 1;";

            var pEmbedding = command.CreateParameter();
            pEmbedding.ParameterName = "@embeddingStr";
            pEmbedding.Value = embeddingString;
            command.Parameters.Add(pEmbedding);

            var pUserId = command.CreateParameter();
            pUserId.ParameterName = "@userId";
            pUserId.Value = currentUserId;
            command.Parameters.Add(pUserId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var similarity = Convert.ToSingle(reader["similarity"]);
                if (similarity >= threshold)
                {
                    var matchedName = reader["full_name"]?.ToString();
                    return (true, matchedName, similarity);
                }
            }

            return (false, null, 0f);
        }
        finally
        {
            if (openedHere)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task RevokeFaceProfileAsync(int faceProfileId, DateTime revokedAt, CancellationToken cancellationToken = default)
    {
        var profile = await _context.FaceProfiles.FirstOrDefaultAsync(p => p.FaceProfileId == faceProfileId, cancellationToken);
        if (profile != null)
        {
            profile.Status = "REVOKED";
            profile.RevokedAt = revokedAt;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
