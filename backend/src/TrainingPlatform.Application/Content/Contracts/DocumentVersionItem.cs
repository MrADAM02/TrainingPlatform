namespace TrainingPlatform.Application.Content.Contracts;

/// <summary><paramref name="VersionId"/> is null for the current version — the frontend uses the
/// existing document download-url endpoint for that one, and this endpoint only for archived
/// versions.</summary>
public sealed record DocumentVersionItem(
    Guid? VersionId,
    int Version,
    bool IsCurrent,
    long SizeBytes,
    DateTime UploadedAtUtc,
    string? UploadedByEmail,
    string? UploadedByFullName);
