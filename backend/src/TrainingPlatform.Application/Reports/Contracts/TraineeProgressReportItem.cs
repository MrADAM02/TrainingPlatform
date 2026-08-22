using TrainingPlatform.Domain.Enrollments;

namespace TrainingPlatform.Application.Reports.Contracts;

public sealed record TraineeProgressReportItem(
    Guid UserId,
    string UserEmail,
    string UserFullName,
    EnrollmentStatus Status,
    DateTime EnrolledAtUtc,
    int CompletedDocuments,
    int TotalDocuments,
    int RequiredQuizzesPassed,
    int RequiredQuizzesTotal,
    bool CertificateIssued);
