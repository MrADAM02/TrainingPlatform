namespace TrainingPlatform.Domain.Content;

public sealed class Document
{
    public Guid Id { get; private set; }

    public Guid ModuleId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DocumentType FileType { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Object storage key. The file itself never touches application disk (REQ-CONT-03).</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public int Version { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    private Document()
    {
    }

    public static Document Create(
        Guid moduleId,
        string title,
        DocumentType fileType,
        string contentType,
        string storageKey,
        long sizeBytes)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            Title = title,
            FileType = fileType,
            ContentType = contentType,
            StorageKey = storageKey,
            SizeBytes = sizeBytes,
            Version = 1,
            UploadedAtUtc = DateTime.UtcNow,
        };
    }

    public static DocumentType InferFileType(string contentType)
    {
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Pdf;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Video;
        }

        if (contentType.Contains("presentation", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Presentation;
        }

        return DocumentType.Other;
    }
}
