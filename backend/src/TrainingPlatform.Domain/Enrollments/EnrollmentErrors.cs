using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Enrollments;

public static class EnrollmentErrors
{
    public static Error NotFound(Guid enrollmentId) => Error.NotFound(
        "Enrollments.NotFound", $"Enrollment '{enrollmentId}' was not found.");

    public static readonly Error AlreadyEnrolled = Error.Conflict(
        "Enrollments.AlreadyEnrolled", "This user is already enrolled in this course.");

    public static readonly Error NotEnrolled = Error.Forbidden(
        "Enrollments.NotEnrolled", "You are not enrolled in this course.");
}
