namespace TrainingPlatform.Application.Quizzes.Contracts;

public sealed record AnswerInput(Guid QuestionId, Guid SelectedChoiceId);
