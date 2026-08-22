namespace TrainingPlatform.Application.Quizzes.Contracts;

public sealed record ChoiceManagementDetails(Guid Id, string Text, bool IsCorrect, int Order);

public sealed record QuestionManagementDetails(Guid Id, string Text, int Order, IReadOnlyList<ChoiceManagementDetails> Choices);

public sealed record QuizManagementDetails(
    Guid Id,
    Guid ModuleId,
    string Title,
    int PassingScorePercent,
    bool IsRequiredForCompletion,
    IReadOnlyList<QuestionManagementDetails> Questions);
