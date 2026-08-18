using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Dashboard.GetMyDashboard;

namespace TrainingPlatform.Api.Endpoints.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/dashboard", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyDashboardQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        })
        .WithTags("Dashboard")
        .RequireAuthorization();

        return app;
    }
}
