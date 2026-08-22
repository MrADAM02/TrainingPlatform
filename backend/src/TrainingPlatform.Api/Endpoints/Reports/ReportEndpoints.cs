using System.Globalization;
using System.Text;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Reports.Contracts;
using TrainingPlatform.Application.Reports.GetCourseCompletionReport;
using TrainingPlatform.Application.Reports.GetOrgSummaryReport;
using TrainingPlatform.Application.Reports.GetTraineeProgressReport;

namespace TrainingPlatform.Api.Endpoints.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var summary = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization("RequireAdministrator");

        summary.MapGet("/summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrgSummaryReportQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        var courses = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        courses.MapGet("/courses", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCourseCompletionReportQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        courses.MapGet("/courses/export", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCourseCompletionReportQuery(), ct);
            if (!result.IsSuccess)
            {
                return CustomResults.Problem(result);
            }

            var csv = BuildCourseCsv(result.Value);
            var fileName = $"course-completion-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        });

        courses.MapGet("/courses/{courseId:guid}/trainees", async (Guid courseId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTraineeProgressReportQuery(courseId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        return app;
    }

    private static string BuildCourseCsv(IReadOnlyList<CourseCompletionReportItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CourseTitle,Published,Enrolled,Completed,CompletionPercent,AvgCompletionDays");

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(',',
                CsvField(item.CourseTitle),
                item.IsPublished ? "Yes" : "No",
                item.EnrolledCount.ToString(CultureInfo.InvariantCulture),
                item.CompletedCount.ToString(CultureInfo.InvariantCulture),
                item.CompletionPercent.ToString(CultureInfo.InvariantCulture),
                item.AvgCompletionDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
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
