using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Bookmarks.AddBookmark;
using TrainingPlatform.Application.Content.Bookmarks.GetMyBookmarks;
using TrainingPlatform.Application.Content.Bookmarks.RemoveBookmark;
using TrainingPlatform.Application.Content.Courses.CreateCourse;
using TrainingPlatform.Application.Content.Courses.DeleteCourse;
using TrainingPlatform.Application.Content.Courses.GetCourseById;
using TrainingPlatform.Application.Content.Courses.GetCourses;
using TrainingPlatform.Application.Content.Courses.PublishCourse;
using TrainingPlatform.Application.Content.Courses.UpdateCourse;

namespace TrainingPlatform.Api.Endpoints.Courses;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/courses").WithTags("Courses").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20, bool mine = false) =>
        {
            var result = await sender.Send(new GetCoursesQuery(page, pageSize, mine), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCourseByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapPost("/", async (CreateCourseCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/courses/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCourseRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateCourseCommand(id, request.Title, request.Description), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        group.MapPost("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PublishCourseCommand(id, true), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        group.MapPost("/{id:guid}/unpublish", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PublishCourseCommand(id, false), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCourseCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        group.MapPost("/{id:guid}/bookmark", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddBookmarkCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        group.MapDelete("/{id:guid}/bookmark", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveBookmarkCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        var bookmarks = app.MapGroup("/api/v1/bookmarks").WithTags("Courses").RequireAuthorization();

        bookmarks.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyBookmarksQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        return app;
    }
}

public sealed record UpdateCourseRequest(string Title, string Description);
