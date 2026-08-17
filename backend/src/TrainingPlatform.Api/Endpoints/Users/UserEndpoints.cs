using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Users.CreateUser;
using TrainingPlatform.Application.Users.DeleteUser;
using TrainingPlatform.Application.Users.GetUserById;
using TrainingPlatform.Application.Users.GetUsers;
using TrainingPlatform.Application.Users.ResetUserPassword;
using TrainingPlatform.Application.Users.SetUserActiveStatus;
using TrainingPlatform.Application.Users.UpdateUser;

namespace TrainingPlatform.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users").RequireAuthorization("RequireAdministrator");

        group.MapGet("/", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            var query = new GetUsersQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapPost("/", async (CreateUserCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateUserCommand(id, request.FullName), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetUserActiveStatusCommand(id, true), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetUserActiveStatusCommand(id, false), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapPost("/{id:guid}/reset-password", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetUserPasswordCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(new { temporaryPassword = result.Value })
                : CustomResults.Problem(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteUserCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        return app;
    }
}

public sealed record UpdateUserRequest(string FullName);
