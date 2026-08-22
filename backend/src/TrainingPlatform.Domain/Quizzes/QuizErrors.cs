using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Quizzes;

public static class QuizErrors
{
    public static Error QuizNotFound(Guid quizId) => Error.NotFound(
        "Quizzes.QuizNotFound", $"Quiz '{quizId}' was not found.");

    public static readonly Error InvalidAnswers = Error.Validation(
        "Quizzes.InvalidAnswers", "Every question must be answered with a valid choice from that question.");
}
