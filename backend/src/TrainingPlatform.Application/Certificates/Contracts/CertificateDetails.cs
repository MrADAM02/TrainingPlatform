namespace TrainingPlatform.Application.Certificates.Contracts;

public sealed record CertificateDetails(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string RecipientFullName,
    string CertificateNumber,
    DateTime IssuedAtUtc);
