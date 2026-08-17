using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Activity.GetActivityLog;

namespace TrainingPlatform.Api.Endpoints.Activity;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/activity-log").WithTags("Activity").RequireAuthorization("RequireAdministrator");

        group.MapGet("/", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetActivityLogQuery(page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        return app;
    }
}
