using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Contracts;

public sealed record DocumentSummary(
    Guid Id,
    Guid ModuleId,
    string Title,
    DocumentType FileType,
    string ContentType,
    long SizeBytes,
    int Version,
    DateTime UploadedAtUtc,
    string? TranscriptText,
    string? SummaryText,
    string? KeyTakeaway,
    int? DurationMinutes,
    bool IsCompleted);
