namespace TrainingPlatform.Domain.Content;

public enum DocumentType
{
    Pdf,
    Video,
    Presentation,
    Other,

    // Appended, not inserted — existing rows already store real ordinal values for the four
    // members above (2026-08-25 lesson-type redesign).
    Image,

    /// <summary>A fileless lesson — see <see cref="Document.CreateTextLesson"/>. Unlike every
    /// other member, a Text document has no <c>StorageKey</c>/<c>ContentType</c>/<c>SizeBytes</c>.</summary>
    Text,
}
