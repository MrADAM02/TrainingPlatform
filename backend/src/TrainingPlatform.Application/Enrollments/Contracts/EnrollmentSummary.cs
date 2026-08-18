using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Enrollments.Contracts;

public sealed record EnrollmentSummary(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    Guid CourseId,
    EnrollmentStatus Status,
    DateTime EnrolledAtUtc);
