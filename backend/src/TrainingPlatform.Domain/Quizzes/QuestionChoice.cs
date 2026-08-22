namespace TrainingPlatform.Domain.Quizzes;

public sealed class QuestionChoice
{
    public Guid Id { get; private set; }

    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int Order { get; private set; }

    private QuestionChoice()
    {
    }

    public static QuestionChoice Create(Guid questionId, string text, bool isCorrect, int order)
    {
        return new QuestionChoice
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Text = text,
            IsCorrect = isCorrect,
            Order = order,
        };
    }
}
