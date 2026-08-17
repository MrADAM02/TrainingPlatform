namespace TrainingPlatform.Domain.Activity;

/// <summary>
/// Immutable audit record of a login or admin action (REQ-ADM-06). Document
/// view/download entries will be added once the content model exists (Phase 2).
/// </summary>
public sealed class ActivityLog
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public required string Action { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? IpAddress { get; init; }

    public DateTime TimestampUtc { get; private set; }

    private ActivityLog()
    {
    }

    public static ActivityLog Create(Guid userId, string action, string? entityType, string? entityId, string? ipAddress)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            TimestampUtc = DateTime.UtcNow,
        };
    }
}
