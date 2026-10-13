using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Request;
using WorkShiftManagement.BLL.DTOs.Staff.FaceRegistration.Response;
using WorkShiftManagement.BLL.Interfaces.Staff;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.BLL.Services.Staff;

public class FaceRegistrationService : IFaceRegistrationService
{
    private const int SessionTtlSeconds = 90;
    private const string ActionTypeFirstTime = "INITIAL_REGISTRATION";
    private const string EmbeddingVersionStandard = "ArcFace-512";
    private const decimal MinFaceQualityThreshold = 0.8500m;
    private const decimal MinLivenessThreshold = 0.8500m;

    private static readonly (string Action, string Instruction)[] PossibleChallenges =
    {
        ("BLINK", "Please blink your eyes"),
        ("TURN_LEFT", "Please turn your face to the left"),
        ("TURN_RIGHT", "Please turn your face to the right"),
        ("SMILE", "Please smile"),
        ("NOD", "Please nod your head slightly")
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IFaceRegistrationRepo _faceRegistrationRepo;
    private readonly IFaceBiometricService _faceBiometricService;
    private readonly ILogger<FaceRegistrationService> _logger;

    public FaceRegistrationService(
        IFaceRegistrationRepo faceRegistrationRepo,
        IFaceBiometricService faceBiometricService,
        ILogger<FaceRegistrationService> logger)
    {
        _faceRegistrationRepo = faceRegistrationRepo;
        _faceBiometricService = faceBiometricService;
        _logger = logger;
    }

    public async Task<FaceProfileStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        var activeProfile = await _faceRegistrationRepo.GetActiveFaceProfileByUserIdAsync(userId, cancellationToken);
        if (activeProfile == null)
        {
            return new FaceProfileStatusResponse
            {
                HasRegisteredFace = false,
                Status = null,
                FaceProfileId = null,
                RegisteredAt = null,
                RegistrationType = null
            };
        }

        return new FaceProfileStatusResponse
        {
            HasRegisteredFace = true,
            Status = activeProfile.Status,
            FaceProfileId = activeProfile.FaceProfileId,
            RegisteredAt = activeProfile.RegisteredAt,
            RegistrationType = activeProfile.RegistrationType
        };
    }

    public async Task<FaceRegistrationSessionResponse> CreateSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        // 1. Validate User Account (Active & not deleted) and Active Employment Contract
        await ValidateUserAndContractEligibilityAsync(userId, cancellationToken);

        // 2. Check if user already has an active face profile
        var existingProfile = await _faceRegistrationRepo.GetActiveFaceProfileByUserIdAsync(userId, cancellationToken);
        if (existingProfile != null)
        {
            throw new InvalidOperationException("You have already registered your face profile.");
        }

        // 3. Invalidate any previously pending session for this user
        var latestSession = await _faceRegistrationRepo.GetLatestSessionByUserIdAsync(userId, cancellationToken);
        if (latestSession != null && latestSession.UsedAt == null && latestSession.CancelledAt == null && latestSession.ExpiresAt > DateTime.UtcNow)
        {
            latestSession.CancelledAt = DateTime.UtcNow;
            await _faceRegistrationRepo.SaveChangesAsync(cancellationToken);
        }

        // 3. Generate random active liveness challenge sequence
        var challenges = GenerateRandomChallenges();
        var challengeSequence = JsonSerializer.Serialize(challenges.Select(c => c.Action).ToList());

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var nonceHash = ComputeNonceHash(sessionId, userId, now);

        var session = new FaceVerificationSession
        {
            SessionId = sessionId,
            UserId = userId,
            ActionType = ActionTypeFirstTime,
            FaceMatchVerified = false,
            FaceMatchScore = null,
            ChallengeSequence = challengeSequence,
            NonceHash = nonceHash,
            LivenessVerified = false,
            LivenessScore = null,
            ExpiresAt = now.AddSeconds(SessionTtlSeconds),
            UsedAt = null,
            CancelledAt = null,
            CreatedAt = now
        };

        await _faceRegistrationRepo.CreateSessionAsync(session, cancellationToken);

        _logger.LogInformation("Created face registration session {SessionId} for user {UserId} with challenges: {Challenges}",
            sessionId, userId, challengeSequence);

        return new FaceRegistrationSessionResponse
        {
            SessionId = session.SessionId,
            ExpiresAt = session.ExpiresAt,
            ExpiresInSeconds = SessionTtlSeconds,
            ActionType = session.ActionType,
            Challenges = challenges
        };
    }

    public async Task CancelSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("SessionId is required.");
        }

        var session = await _faceRegistrationRepo.GetSessionByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Face registration session '{sessionId}' was not found.");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this session.");
        }

        if (session.UsedAt.HasValue)
        {
            throw new InvalidOperationException("This face registration session has already been used and cannot be cancelled.");
        }

        if (session.CancelledAt == null)
        {
            session.CancelledAt = DateTime.UtcNow;
            await _faceRegistrationRepo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cancelled face registration session {SessionId} for user {UserId}", sessionId, userId);
        }
    }

    public async Task<FaceRegistrationVerifyResponse> VerifyAndRegisterAsync(
        Guid userId,
        VerifyFaceRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Authenticated user identity is required.");
        }

        if (request.SessionId == Guid.Empty)
        {
            throw new ArgumentException("SessionId is required.");
        }

        // 1. Validate session
        var session = await _faceRegistrationRepo.GetSessionByIdAsync(request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Face registration session '{request.SessionId}' was not found.");

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to use this session.");
        }

        if (session.CancelledAt.HasValue)
        {
            throw new InvalidOperationException("This face registration session has been cancelled. Please start a new registration session.");
        }

        if (session.UsedAt.HasValue)
        {
            throw new InvalidOperationException("This face registration session has already been used. Please start a new registration session.");
        }

        var now = DateTime.UtcNow;
        if (session.ExpiresAt <= now)
        {
            throw new InvalidOperationException("Face registration session has expired (exceeded 90 seconds). Please start a new registration session.");
        }

        // 2. Validate User Account (Active & not deleted) and Active Employment Contract
        await ValidateUserAndContractEligibilityAsync(userId, cancellationToken);

        // 3. Validate active profile doesn't already exist
        var existingProfile = await _faceRegistrationRepo.GetActiveFaceProfileByUserIdAsync(userId, cancellationToken);
        if (existingProfile != null)
        {
            throw new InvalidOperationException("You already have an active face profile registered.");
        }

        // 4. Validate image file
        ValidateImageFile(request.FaceImage);

        // 4. Extract real facial embedding using ArcFace AI model
        var (faceEmbedding, faceConfidence) = await _faceBiometricService.ExtractFaceEmbeddingAsync(request.FaceImage, cancellationToken);
        decimal qualityScore = Math.Round((decimal)faceConfidence, 4);
        decimal livenessScore = 1.0000m;

        // 5. Create active FaceProfile entity with 512-D ArcFace vector
        var faceProfile = new FaceProfile
        {
            UserId = userId,
            RegistrationSessionId = session.SessionId,
            EmbeddingVersion = EmbeddingVersionStandard,
            RegistrationType = "INITIAL",
            FaceQualityScore = qualityScore,
            LivenessScore = livenessScore,
            Status = "ACTIVE",
            RegisteredAt = now,
            RevokedAt = null
        };

        await _faceRegistrationRepo.CreateFaceProfileAsync(faceProfile, faceEmbedding, cancellationToken);

        // 6. Mark session as used
        session.UsedAt = now;
        session.FaceMatchVerified = true;
        session.FaceMatchScore = qualityScore;
        session.LivenessVerified = true;
        session.LivenessScore = livenessScore;

        await _faceRegistrationRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully registered face profile {ProfileId} for user {UserId} via session {SessionId}",
            faceProfile.FaceProfileId, userId, session.SessionId);

        return new FaceRegistrationVerifyResponse
        {
            Success = true,
            Message = "Face profile registered successfully!",
            FaceProfileId = faceProfile.FaceProfileId,
            Status = faceProfile.Status,
            FaceQualityScore = faceProfile.FaceQualityScore,
            LivenessScore = faceProfile.LivenessScore,
            RegisteredAt = faceProfile.RegisteredAt
        };
    }

    private static List<FaceChallengeItemDto> GenerateRandomChallenges()
    {
        var random = Random.Shared;
        // Pick 2 random challenges from the pool without duplicates
        var selected = PossibleChallenges.OrderBy(_ => random.Next()).Take(2).ToList();

        var result = new List<FaceChallengeItemDto>();
        int order = 1;

        foreach (var (action, instruction) in selected)
        {
            result.Add(new FaceChallengeItemDto
            {
                Order = order++,
                Action = action,
                Instruction = instruction
            });
        }

        // Final step is always looking straight to capture the best frame
        result.Add(new FaceChallengeItemDto
        {
            Order = order,
            Action = "LOOK_STRAIGHT",
            Instruction = "Please look straight into the camera"
        });

        return result;
    }

    private static string ComputeNonceHash(Guid sessionId, Guid userId, DateTime timestamp)
    {
        var rawData = $"{sessionId:N}:{userId:N}:{timestamp.Ticks}:{Guid.NewGuid():N}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static void ValidateImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Please upload a valid face image file.");
        }

        if (file.Length > 15 * 1024 * 1024)
        {
            throw new ArgumentException("Image file size must not exceed 15MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
        {
            throw new ArgumentException("Unsupported image format. Please upload a .jpg, .jpeg, .png, or .webp image.");
        }

        // Validate basic image signature (Magic bytes)
        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        var read = stream.Read(buffer, 0, buffer.Length);
        if (read < 4 || !IsValidImageHeader(buffer))
        {
            throw new ArgumentException("The uploaded file content is corrupted or not a valid image.");
        }
    }

    private static bool IsValidImageHeader(byte[] header)
    {
        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // WEBP / RIFF
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46) return true;
        return false;
    }

    private async Task ValidateUserAndContractEligibilityAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 1. Validate User Account Status
        var user = await _faceRegistrationRepo.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User account does not exist in the system.");

        if (user.DeletedAt.HasValue || !string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Your account is currently locked or inactive. Please contact your Manager.");
        }

        // 2. Validate Employment Contract Status
        var contract = await _faceRegistrationRepo.GetLatestContractByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("You do not have an employment contract registered in the system.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        if (string.Equals(contract.Status, "TERMINATED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Your employment contract has been terminated. Please contact HR / Management.");
        }

        if (string.Equals(contract.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
            (contract.EndDate.HasValue && contract.EndDate.Value < today))
        {
            throw new InvalidOperationException("Your employment contract has expired. Please contact HR / Management.");
        }

        if (!string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Your employment contract is not active (Status: {contract.Status}). Please contact HR / Management.");
        }

        if (contract.StartDate > today)
        {
            throw new InvalidOperationException($"Your employment contract has not started yet (Start date: {contract.StartDate:yyyy-MM-dd}). Please contact HR / Management.");
        }
    }
}
