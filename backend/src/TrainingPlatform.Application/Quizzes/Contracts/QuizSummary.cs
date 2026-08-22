namespace TrainingPlatform.Application.Quizzes.Contracts;

public sealed record QuizSummary(
    Guid Id,
    Guid ModuleId,
    string Title,
    int PassingScorePercent,
    bool IsRequiredForCompletion);
