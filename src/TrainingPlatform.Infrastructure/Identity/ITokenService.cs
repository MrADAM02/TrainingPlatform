using System.Security.Claims;

namespace TrainingPlatform.Infrastructure.Identity;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles);

    string GenerateRefreshTokenValue();

    string Hash(string value);
}
