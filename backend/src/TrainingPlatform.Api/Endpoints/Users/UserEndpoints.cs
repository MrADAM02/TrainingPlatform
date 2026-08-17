using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Users.CreateUser;

namespace TrainingPlatform.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users").RequireAuthorization("RequireAdministrator");

        group.MapPost("/", async (CreateUserCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        return app;
    }
}
