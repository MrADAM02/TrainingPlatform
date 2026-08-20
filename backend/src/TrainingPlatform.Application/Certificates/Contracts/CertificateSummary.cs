namespace TrainingPlatform.Application.Certificates.Contracts;

public sealed record CertificateSummary(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string CertificateNumber,
    DateTime IssuedAtUtc);
