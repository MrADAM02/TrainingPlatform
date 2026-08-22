namespace TrainingPlatform.Domain.Quizzes;

public sealed class Question
{
    public Guid Id { get; private set; }

    public Guid QuizId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public int Order { get; private set; }

    public QuestionType Type { get; private set; }

    private Question()
    {
    }

    public static Question Create(Guid quizId, string text, int order)
    {
        return new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            Text = text,
            Order = order,
            Type = QuestionType.SingleChoice,
        };
    }
}
