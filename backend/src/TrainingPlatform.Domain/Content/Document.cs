namespace TrainingPlatform.Domain.Content;

public sealed class Document
{
    public Guid Id { get; private set; }

    public Guid ModuleId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DocumentType FileType { get; private set; }

    /// <summary>Null only for <see cref="DocumentType.Text"/> — a fileless lesson has no content
    /// type. Every other type always has one.</summary>
    public string? ContentType { get; private set; }

    /// <summary>Object storage key. The file itself never touches application disk (REQ-CONT-03).
    /// Null only for <see cref="DocumentType.Text"/> — see <see cref="ContentType"/>.</summary>
    public string? StorageKey { get; private set; }

    /// <summary>Null only for <see cref="DocumentType.Text"/> — see <see cref="ContentType"/>.</summary>
    public long? SizeBytes { get; private set; }

    public int Version { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    /// <summary>No FK — <c>ApplicationUser</c> lives in Infrastructure, which Domain must not
    /// depend on. Nullable because documents created before this field existed have no recorded
    /// uploader.</summary>
    public Guid? UploadedByUserId { get; private set; }

    /// <summary>Lesson-detail fields (2026-08-24 redesign), meaningful for <see
    /// cref="DocumentType.Video"/> documents but not type-enforced at this layer — a non-video
    /// document simply never gets these set. All optional; trainer-authored.</summary>
    public string? TranscriptText { get; private set; }

    public string? SummaryText { get; private set; }

    public string? KeyTakeaway { get; private set; }

    /// <summary>Trainer-entered video length in minutes. Deliberately not auto-detected from the
    /// file (no video-inspection library in this stack) — left null until a trainer sets it, so
    /// downstream stats (e.g. the learning-streak "hours" figure) only ever sum real, trainer-
    /// provided durations rather than a fabricated estimate.</summary>
    public int? DurationMinutes { get; private set; }

    /// <summary>Trainer-entered PDF page count (2026-08-25 lesson-type redesign) — same reasoning
    /// as <see cref="DurationMinutes"/>: no PDF-parsing capability exists in this stack, so this
    /// stays null until a trainer sets it rather than being guessed from file size.</summary>
    public int? PageCount { get; private set; }

    /// <summary>Pull-quote, meaningful for <see cref="DocumentType.Text"/> lessons.</summary>
    public string? Quote { get; private set; }

    private Document()
    {
    }

    public static Document Create(
        Guid moduleId,
        string title,
        DocumentType fileType,
        string contentType,
        string storageKey,
        long sizeBytes,
        Guid? uploadedByUserId)
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
            UploadedByUserId = uploadedByUserId,
        };
    }

    /// <summary>A fileless lesson (2026-08-25 lesson-type redesign) — <see cref="TranscriptText"/>
    /// doubles as the lesson body here, same "written content of the lesson" semantics as a video
    /// transcript, just with nothing to transcribe. No <see cref="ContentType"/>/<see
    /// cref="StorageKey"/>/<see cref="SizeBytes"/>, since there is no file.</summary>
    public static Document CreateTextLesson(Guid moduleId, string title, string bodyText, string? quote, Guid? uploadedByUserId)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            Title = title,
            FileType = DocumentType.Text,
            TranscriptText = bodyText,
            Quote = quote,
            Version = 1,
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId,
        };
    }

    /// <summary>Replaces the current file with a new one (REQ-CONT-06), bumping <see
    /// cref="Version"/>. The caller is responsible for archiving the pre-replacement state as a
    /// <see cref="DocumentVersion"/> before calling this, since this method only knows the new
    /// state.</summary>
    public void ReplaceFile(DocumentType fileType, string contentType, string storageKey, long sizeBytes, Guid? uploadedByUserId)
    {
        FileType = fileType;
        ContentType = contentType;
        StorageKey = storageKey;
        SizeBytes = sizeBytes;
        Version += 1;
        UploadedAtUtc = DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
    }

    public void UpdateLessonDetails(
        string title, string? transcriptText, string? summaryText, string? keyTakeaway,
        int? durationMinutes, int? pageCount, string? quote)
    {
        Title = title;
        TranscriptText = transcriptText;
        SummaryText = summaryText;
        KeyTakeaway = keyTakeaway;
        DurationMinutes = durationMinutes;
        PageCount = pageCount;
        Quote = quote;
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

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Image;
        }

        if (contentType.Contains("presentation", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Presentation;
        }

        return DocumentType.Other;
    }
}
