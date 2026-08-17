using Microsoft.AspNetCore.Http;
using TrainingPlatform.Application.Abstractions.Http;

namespace TrainingPlatform.Infrastructure.Http;

public sealed class ClientContext(IHttpContextAccessor httpContextAccessor) : IClientContext
{
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
