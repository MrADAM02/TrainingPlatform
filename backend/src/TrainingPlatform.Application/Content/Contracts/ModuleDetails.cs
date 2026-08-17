namespace TrainingPlatform.Application.Content.Contracts;

public sealed record ModuleDetails(
    Guid Id,
    Guid CourseId,
    string Title,
    int Order,
    IReadOnlyList<DocumentSummary> Documents);
