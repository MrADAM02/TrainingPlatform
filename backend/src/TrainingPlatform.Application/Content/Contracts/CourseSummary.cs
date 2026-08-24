namespace TrainingPlatform.Application.Content.Contracts;

public sealed record CourseSummary(
    Guid Id,
    string Title,
    string Description,
    Guid TrainerId,
    bool IsPublished,
    DateTime CreatedAtUtc,
    bool IsEnrolled,
    bool IsBookmarked);
