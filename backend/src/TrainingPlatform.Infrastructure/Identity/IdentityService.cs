using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;
using TrainingPlatform.Infrastructure.Database;

namespace TrainingPlatform.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    IActivityLogService activityLogService,
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

        await activityLogService.LogAsync(
            user.Id, ActivityActions.UserLoggedIn, "User", user.Id.ToString(), cancellationToken);

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

    public async Task<Result<PaginatedList<UserSummary>>> GetUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking().OrderByDescending(u => u.CreatedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);

        var pagedUsers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = pagedUsers.Select(u => u.Id).ToList();

        var rolesByUser = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name! }
        ).ToListAsync(cancellationToken);

        var items = pagedUsers
            .Select(u => new UserSummary(
                u.Id,
                u.Email!,
                u.FullName,
                rolesByUser.Where(r => r.UserId == u.Id).Select(r => r.RoleName).ToList(),
                u.IsActive,
                u.CreatedAtUtc,
                u.LastLoginAtUtc))
            .ToList();

        return new PaginatedList<UserSummary>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<Result<UserSummary>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<UserSummary>(UserErrors.NotFound(userId));
        }

        var roles = await userManager.GetRolesAsync(user);

        return new UserSummary(user.Id, user.Email!, user.FullName, [.. roles], user.IsActive, user.CreatedAtUtc, user.LastLoginAtUtc);
    }

    public async Task<Result<IReadOnlyList<UserSummary>>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var ids = users.Select(u => u.Id).ToList();

        var rolesByUser = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name! }
        ).ToListAsync(cancellationToken);

        IReadOnlyList<UserSummary> items = users
            .Select(u => new UserSummary(
                u.Id,
                u.Email!,
                u.FullName,
                rolesByUser.Where(r => r.UserId == u.Id).Select(r => r.RoleName).ToList(),
                u.IsActive,
                u.CreatedAtUtc,
                u.LastLoginAtUtc))
            .ToList();

        return Result.Success(items);
    }

    public async Task<Result<IReadOnlyList<UserSummary>>> SearchTraineesAsync(
        string? keyword, int limit, CancellationToken cancellationToken)
    {
        var query =
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == Roles.Trainee
            select user;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = keyword.ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(pattern) || u.FullName.ToLower().Contains(pattern));
        }

        var users = await query.OrderBy(u => u.Email).Take(limit).ToListAsync(cancellationToken);

        IReadOnlyList<UserSummary> items = users
            .Select(u => new UserSummary(u.Id, u.Email!, u.FullName, [Roles.Trainee], u.IsActive, u.CreatedAtUtc, u.LastLoginAtUtc))
            .ToList();

        return Result.Success(items);
    }

    public async Task<Result> UpdateUserAsync(Guid userId, string fullName, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        user.FullName = fullName;
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> SetUserActiveStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure(ToError(result));
        }

        if (!isActive)
        {
            await RevokeAllActiveTokensAsync(userId, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        var result = await userManager.DeleteAsync(user);

        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result<string>> AdminResetPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<string>(UserErrors.NotFound(userId));
        }

        var temporaryPassword = GenerateTemporaryPassword();

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
        if (!result.Succeeded)
        {
            return Result.Failure<string>(ToError(result));
        }

        await RevokeAllActiveTokensAsync(userId, cancellationToken);

        return temporaryPassword;
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

    private static string GenerateTemporaryPassword()
    {
        var randomPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(9))
            .Replace("+", "A").Replace("/", "B").Replace("=", "");

        return $"Tmp-{randomPart}1!";
    }

    private static Error ToError(IdentityResult result)
    {
        var description = string.Join(" ", result.Errors.Select(e => e.Description));
        return Error.Failure("Users.IdentityError", description);
    }
}
