namespace TrainingPlatform.Domain.Content;

/// <summary>A superseded version of a <see cref="Document"/> (REQ-CONT-06), archived just before
/// a replace. The current version's data lives on <see cref="Document"/> itself, not here — this
/// table only ever holds prior versions, so a document with no replace history has zero rows
/// here. Cascade-deletes with its parent document: unlike ActivityLog/Certificate/QuizAttempt,
/// version history has no reason to outlive the document it belongs to.</summary>
public sealed class DocumentVersion
{
    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public int Version { get; private set; }

    public DocumentType FileType { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>No FK — same reasoning as <see cref="Document.UploadedByUserId"/>.</summary>
    public Guid? UploadedByUserId { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    private DocumentVersion()
    {
    }

    public static DocumentVersion Create(
        Guid documentId,
        int version,
        DocumentType fileType,
        string contentType,
        string storageKey,
        long sizeBytes,
        Guid? uploadedByUserId,
        DateTime uploadedAtUtc)
    {
        return new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Version = version,
            FileType = fileType,
            ContentType = contentType,
            StorageKey = storageKey,
            SizeBytes = sizeBytes,
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = uploadedAtUtc,
        };
    }
}
