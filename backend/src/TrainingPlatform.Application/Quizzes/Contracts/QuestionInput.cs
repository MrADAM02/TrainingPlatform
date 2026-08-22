namespace TrainingPlatform.Application.Quizzes.Contracts;

public sealed record ChoiceInput(string Text, bool IsCorrect, int Order);

public sealed record QuestionInput(string Text, int Order, IReadOnlyList<ChoiceInput> Choices);
