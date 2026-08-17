namespace TrainingPlatform.Application.Abstractions.Authentication;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
