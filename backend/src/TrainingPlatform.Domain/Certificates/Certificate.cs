namespace TrainingPlatform.Domain.Certificates;

/// <summary>
/// Issued automatically when a Trainee completes a course (every document consumed — see
/// <c>GetDocumentDownloadUrlQuery</c>). Deliberately has no FK to Course/Enrollment/User: a
/// certificate is a permanent achievement record and must survive deletion of the course,
/// enrollment, or even the account it was issued to, the same reasoning as <c>ActivityLog</c>.
/// Course title and recipient name are snapshotted at issue time so the certificate still reads
/// correctly even if the course is later renamed or deleted.
/// </summary>
public sealed class Certificate
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public required string CourseTitle { get; init; }

    public required string RecipientFullName { get; init; }

    public required string CertificateNumber { get; init; }

    public DateTime IssuedAtUtc { get; private set; }

    private Certificate()
    {
    }

    public static Certificate Create(Guid userId, Guid courseId, string courseTitle, string recipientFullName)
    {
        return new Certificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            CourseTitle = courseTitle,
            RecipientFullName = recipientFullName,
            CertificateNumber = GenerateCertificateNumber(),
            IssuedAtUtc = DateTime.UtcNow,
        };
    }

    private static string GenerateCertificateNumber()
    {
        // A short, human-typeable code for display on the printed certificate — not a raw GUID.
        return $"CERT-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
