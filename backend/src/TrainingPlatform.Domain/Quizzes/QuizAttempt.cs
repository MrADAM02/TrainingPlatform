namespace TrainingPlatform.Domain.Quizzes;

/// <summary>
/// Deliberately no FK to the user it belongs to — same reasoning as ActivityLog/Certificate: an
/// attempt is a permanent record of what a trainee did and must survive account deletion.
/// </summary>
public sealed class QuizAttempt
{
    public Guid Id { get; private set; }

    public Guid QuizId { get; private set; }

    public Guid UserId { get; private set; }

    public int ScorePercent { get; private set; }

    public bool Passed { get; private set; }

    public DateTime AttemptedAtUtc { get; private set; }

    private QuizAttempt()
    {
    }

    public static QuizAttempt Create(Guid quizId, Guid userId, int scorePercent, int passingScorePercent)
    {
        return new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            UserId = userId,
            ScorePercent = scorePercent,
            Passed = scorePercent >= passingScorePercent,
            AttemptedAtUtc = DateTime.UtcNow,
        };
    }
}
