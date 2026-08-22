namespace TrainingPlatform.Application.Reports.Contracts;

public sealed record OrgSummaryReport(
    int TotalCourses,
    int PublishedCourses,
    int TotalEnrollments,
    int CompletedEnrollments,
    int TotalCertificatesIssued,
    int ActiveTraineesLast30Days);
