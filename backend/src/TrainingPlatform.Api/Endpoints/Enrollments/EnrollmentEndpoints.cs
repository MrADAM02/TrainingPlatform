using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Enrollments.EnrollUsers;
using TrainingPlatform.Application.Enrollments.GetCourseEnrollments;
using TrainingPlatform.Application.Enrollments.RemoveEnrollment;

namespace TrainingPlatform.Api.Endpoints.Enrollments;

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var courseEnrollments = app.MapGroup("/api/v1/courses/{courseId:guid}/enrollments")
            .WithTags("Enrollments")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        courseEnrollments.MapGet("/", async (Guid courseId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCourseEnrollmentsQuery(courseId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        courseEnrollments.MapPost("/", async (Guid courseId, EnrollUsersRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new EnrollUsersCommand(courseId, request.UserIds), ct);
            return result.IsSuccess ? Results.Ok(new { enrolledCount = result.Value }) : CustomResults.Problem(result);
        });

        app.MapDelete("/api/v1/enrollments/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveEnrollmentCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .WithTags("Enrollments")
        .RequireAuthorization("RequireTrainerOrAdministrator");

        return app;
    }
}

public sealed record EnrollUsersRequest(IReadOnlyList<Guid> UserIds);
