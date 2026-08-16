namespace TrainingPlatform.Infrastructure.Identity;

/// <summary>
/// A rotated, hashed refresh token record. On every refresh the current token is revoked and a
/// new one issued (REQ-AUTH-03). A refresh attempt using an already-revoked token is treated as
/// possible token theft and revokes the entire active chain for that user.
/// </summary>
public sealed class RefreshTokenEntity
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the opaque token value — the raw value is never persisted.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
