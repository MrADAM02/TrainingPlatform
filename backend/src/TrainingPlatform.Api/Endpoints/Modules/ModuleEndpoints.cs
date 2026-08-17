using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Modules.CreateModule;
using TrainingPlatform.Application.Content.Modules.DeleteModule;
using TrainingPlatform.Application.Content.Modules.UpdateModule;

namespace TrainingPlatform.Api.Endpoints.Modules;

public static class ModuleEndpoints
{
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var courseModules = app.MapGroup("/api/v1/courses/{courseId:guid}/modules")
            .WithTags("Modules")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        courseModules.MapPost("/", async (Guid courseId, CreateModuleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateModuleCommand(courseId, request.Title, request.Order), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/modules/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        var modules = app.MapGroup("/api/v1/modules")
            .WithTags("Modules")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        modules.MapPut("/{id:guid}", async (Guid id, UpdateModuleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateModuleCommand(id, request.Title, request.Order), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        modules.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteModuleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        return app;
    }
}

public sealed record CreateModuleRequest(string Title, int Order);

public sealed record UpdateModuleRequest(string Title, int Order);
