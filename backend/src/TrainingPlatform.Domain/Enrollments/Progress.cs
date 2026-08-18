namespace TrainingPlatform.Domain.Enrollments;

/// <summary>Records that an enrolled learner has consumed (downloaded/viewed) a document
/// (REQ-ENR-02).</summary>
public sealed class Progress
{
    public Guid Id { get; private set; }

    public Guid EnrollmentId { get; private set; }

    public Guid DocumentId { get; private set; }

    public DateTime CompletedAtUtc { get; private set; }

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
}
