using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;
using TrainingPlatform.Infrastructure.Database;

namespace TrainingPlatform.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions) : IIdentityService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<Guid>> CreateUserAsync(
        string email,
        string fullName,
        string password,
        string role,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return Result.Failure<Guid>(UserErrors.EmailAlreadyInUse);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return Result.Failure<Guid>(ToError(createResult));
        }

        await userManager.AddToRoleAsync(user, role);

        return user.Id;
    }

    public async Task<Result<AuthTokensResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.AccountDeactivated);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        var roles = await userManager.GetRolesAsync(user);
        var tokens = await IssueTokensAsync(user, roles, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    public async Task<Result<AuthTokensResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.Hash(refreshToken);
        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidOrExpiredRefreshToken);
        }

        if (existing.RevokedAtUtc is not null)
        {
            // Reuse of an already-rotated token: treat as possible theft and kill the whole chain.
            await RevokeAllActiveTokensAsync(existing.UserId, cancellationToken);
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidOrExpiredRefreshToken);
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.InvalidOrExpiredRefreshToken);
        }

        var roles = await userManager.GetRolesAsync(user);
        var tokens = await IssueTokensAsync(user, roles, cancellationToken);

        existing.RevokedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.Hash(refreshToken);
        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(
        ApplicationUser user,
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        var (accessToken, accessTokenExpiresAtUtc) = tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = tokenService.GenerateRefreshTokenValue();
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.Hash(refreshTokenValue),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
        });

        await Task.CompletedTask;

        return new AuthTokensResponse(accessToken, accessTokenExpiresAtUtc, refreshTokenValue, refreshTokenExpiresAtUtc);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Error ToError(IdentityResult result)
    {
        var description = string.Join(" ", result.Errors.Select(e => e.Description));
        return Error.Failure("Users.IdentityError", description);
    }
}
