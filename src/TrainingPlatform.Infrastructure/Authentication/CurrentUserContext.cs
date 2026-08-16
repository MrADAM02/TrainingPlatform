using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TrainingPlatform.Application.Abstractions.Authentication;

namespace TrainingPlatform.Infrastructure.Authentication;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("sub");

            return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];
}
