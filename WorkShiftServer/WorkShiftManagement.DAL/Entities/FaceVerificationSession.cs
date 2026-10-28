namespace WorkShiftManagement.DAL.Entities;

public partial class FaceVerificationSession
{
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public string ActionType { get; set; } = null!;

    public bool FaceMatchVerified { get; set; }

    public decimal? FaceMatchScore { get; set; }

    public string ChallengeSequence { get; set; } = null!;

    public string NonceHash { get; set; } = null!;

    public bool LivenessVerified { get; set; }

    public decimal? LivenessScore { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? MatchedFaceProfileId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual ICollection<Attendance> AttendanceCheckInFaceSessions { get; set; } = new List<Attendance>();

    public virtual ICollection<Attendance> AttendanceCheckOutFaceSessions { get; set; } = new List<Attendance>();

    public virtual FaceProfile? FaceProfile { get; set; }

    public virtual FaceProfile? MatchedFaceProfile { get; set; }

    public virtual User User { get; set; } = null!;
}
