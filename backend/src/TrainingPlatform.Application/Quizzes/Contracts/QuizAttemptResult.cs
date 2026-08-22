namespace TrainingPlatform.Application.Quizzes.Contracts;

public sealed record QuizAttemptResult(
    int ScorePercent,
    bool Passed,
    bool CourseCompleted,
    bool CertificateIssued,
    Guid? CertificateId);
