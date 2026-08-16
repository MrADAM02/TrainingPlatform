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
}
