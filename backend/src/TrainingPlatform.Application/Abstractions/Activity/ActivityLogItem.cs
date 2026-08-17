namespace TrainingPlatform.Application.Abstractions.Activity;

public sealed record ActivityLogItem(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? IpAddress,
    DateTime TimestampUtc);
