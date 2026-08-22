namespace TrainingPlatform.Application.Quizzes.Contracts;

/// <summary>Trainee-facing question/choice text only — IsCorrect is never included in this
/// contract, not just hidden client-side, since it would otherwise be visible via devtools
/// regardless of what the UI does with it.</summary>
public sealed record ChoiceAttemptView(Guid Id, string Text, int Order);

public sealed record QuestionAttemptView(Guid Id, string Text, int Order, IReadOnlyList<ChoiceAttemptView> Choices);

public sealed record QuizAttemptView(
    Guid Id,
    Guid ModuleId,
    string Title,
    int PassingScorePercent,
    IReadOnlyList<QuestionAttemptView> Questions,
    bool HasPassed,
    int? BestScorePercent);
