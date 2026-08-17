namespace TrainingPlatform.Application.Content.Contracts;

public sealed record CourseDetails(
    Guid Id,
    string Title,
    string Description,
    Guid TrainerId,
    bool IsPublished,
    DateTime CreatedAtUtc,
    IReadOnlyList<ModuleDetails> Modules);
