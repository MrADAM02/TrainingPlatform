namespace TrainingPlatform.Domain.Enrollments;

public sealed class Enrollment
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    public DateTime EnrolledAtUtc { get; private set; }

    private Enrollment()
    {
    }

    public static Enrollment Create(Guid userId, Guid courseId)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            EnrolledAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkCompleted() => Status = EnrollmentStatus.Completed;
}
