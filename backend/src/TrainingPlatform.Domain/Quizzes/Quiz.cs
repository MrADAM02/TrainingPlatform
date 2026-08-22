namespace TrainingPlatform.Domain.Quizzes;

public sealed class Quiz
{
    public Guid Id { get; private set; }

    public Guid ModuleId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public int PassingScorePercent { get; private set; }

    public bool IsRequiredForCompletion { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Quiz()
    {
    }

    public static Quiz Create(Guid moduleId, string title, int passingScorePercent, bool isRequiredForCompletion)
    {
        return new Quiz
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            Title = title,
            PassingScorePercent = passingScorePercent,
            IsRequiredForCompletion = isRequiredForCompletion,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateDetails(string title, int passingScorePercent, bool isRequiredForCompletion)
    {
        Title = title;
        PassingScorePercent = passingScorePercent;
        IsRequiredForCompletion = isRequiredForCompletion;
    }
}
