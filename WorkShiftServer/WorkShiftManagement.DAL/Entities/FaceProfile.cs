namespace WorkShiftManagement.DAL.Entities;

public partial class FaceProfile
{
    public int FaceProfileId { get; set; }

    public Guid UserId { get; set; }

    public Guid RegistrationSessionId { get; set; }

    public string EmbeddingVersion { get; set; } = null!;

    public string RegistrationType { get; set; } = null!;

    public decimal? FaceQualityScore { get; set; }

    public decimal LivenessScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RegisteredAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual ICollection<FaceVerificationSession> FaceVerificationSessions { get; set; } = new List<FaceVerificationSession>();

    public virtual FaceVerificationSession RegistrationSession { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
