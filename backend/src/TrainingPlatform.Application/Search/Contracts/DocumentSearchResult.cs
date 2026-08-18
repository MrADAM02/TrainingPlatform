using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Search.Contracts;

public sealed record DocumentSearchResult(
    Guid DocumentId,
    string DocumentTitle,
    DocumentType FileType,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    DateTime UploadedAtUtc,
    bool CanDownload);
