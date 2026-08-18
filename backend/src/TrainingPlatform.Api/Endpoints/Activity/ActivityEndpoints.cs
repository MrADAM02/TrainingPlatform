using System.Text;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Activity.ExportActivityLog;
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

        group.MapGet("/export", async (ISender sender, CancellationToken ct, DateTime? from = null, DateTime? to = null) =>
        {
            var result = await sender.Send(new ExportActivityLogQuery(from, to), ct);
            if (!result.IsSuccess)
            {
                return CustomResults.Problem(result);
            }

            var csv = BuildCsv(result.Value);
            var fileName = $"activity-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        });

        return app;
    }

    private static string BuildCsv(IReadOnlyList<ActivityLogItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Timestamp,ActorEmail,Action,EntityType,EntityId,IpAddress");

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(',',
                item.TimestampUtc.ToString("O"),
                CsvField(item.UserEmail),
                CsvField(item.Action),
                CsvField(item.EntityType),
                CsvField(item.EntityId),
                CsvField(item.IpAddress)));
        }

        return builder.ToString();
    }

    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
