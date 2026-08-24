namespace TrainingPlatform.Domain.Content;

/// <summary>A user's saved course ("My Library", 2026-08-24 redesign). Unlike
/// Certificate/QuizAttempt/ActivityLog's deliberate no-FK permanence pattern, a bookmark has no
/// reason to survive deleting either the course or the account that saved it — real FK cascade to
/// both.</summary>
public sealed class CourseBookmark
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private CourseBookmark()
    {
    }

    public static CourseBookmark Create(Guid userId, Guid courseId)
    {
        return new CourseBookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
