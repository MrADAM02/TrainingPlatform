using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Abstractions.Authentication;

public interface IIdentityService
{
    /// <summary>
    /// Creates a user account. Accounts are provisioned by an Administrator (REQ-ADM-01) —
    /// there is no public self-registration surface in this platform.
    /// </summary>
    Task<Result<Guid>> CreateUserAsync(
        string email,
        string fullName,
        string password,
        string role,
        CancellationToken cancellationToken);

    Task<Result<AuthTokensResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result<AuthTokensResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<Result<PaginatedList<UserSummary>>> GetUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<UserSummary>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Batch lookup used where Application-layer code (e.g. enrollment listings) needs
    /// to resolve a set of user ids to display details — Application cannot query the Identity
    /// store (Infrastructure) directly.</summary>
    Task<Result<IReadOnlyList<UserSummary>>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>Used by the enrollment UI: Trainer/Admin need to find Trainees to enroll, but
    /// the full user directory (<see cref="GetUsersAsync"/>) is Administrator-only.</summary>
    Task<Result<IReadOnlyList<UserSummary>>> SearchTraineesAsync(
        string? keyword, int limit, CancellationToken cancellationToken);

    Task<Result> UpdateUserAsync(Guid userId, string fullName, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivating a user also revokes their active refresh tokens, so the change takes effect
    /// immediately rather than waiting for their session to expire.
    /// </summary>
    Task<Result> SetUserActiveStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Admin-forced password reset (REQ-AUTH-05). Generates a new temporary password and revokes
    /// the user's active sessions. There is no email delivery yet, so the temporary password is
    /// returned to the caller (the admin) to relay out of band.
    /// </summary>
    Task<Result<string>> AdminResetPasswordAsync(Guid userId, CancellationToken cancellationToken);
}
