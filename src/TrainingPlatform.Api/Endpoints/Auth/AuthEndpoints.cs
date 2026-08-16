using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Auth.Login;
using TrainingPlatform.Application.Auth.Logout;
using TrainingPlatform.Application.Auth.RefreshToken;

namespace TrainingPlatform.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth-strict");

        group.MapPost("/refresh-token", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        })
        .AllowAnonymous()
        .RequireRateLimiting("token-refresh");

        group.MapPost("/logout", async (LogoutCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok() : CustomResults.Problem(result);
        })
        .AllowAnonymous();

        return app;
    }
}
