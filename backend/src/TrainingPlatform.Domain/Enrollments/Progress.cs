namespace TrainingPlatform.Domain.Enrollments;

/// <summary>Records that an enrolled learner has consumed (downloaded/viewed) a document
/// (REQ-ENR-02).</summary>
public sealed class Progress
{
    public Guid Id { get; private set; }

    public Guid EnrollmentId { get; private set; }

    public Guid DocumentId { get; private set; }

    public DateTime CompletedAtUtc { get; private set; }

    /// <summary>Last known video playback position, in seconds, so a trainee can resume where
    /// they left off. Null for non-video documents or if playback was never reported.</summary>
    public int? LastPositionSeconds { get; private set; }

    public DateTime? LastWatchedAtUtc { get; private set; }

    private Progress()
    {
    }

    public static Progress Create(Guid enrollmentId, Guid documentId)
    {
        return new Progress
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            DocumentId = documentId,
            CompletedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdatePlaybackPosition(int positionSeconds)
    {
        if (positionSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(positionSeconds), "Position cannot be negative.");
        }

        LastPositionSeconds = positionSeconds;
        LastWatchedAtUtc = DateTime.UtcNow;
    }
}
