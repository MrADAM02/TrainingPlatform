using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Documents.CreateTextLesson;
using TrainingPlatform.Application.Content.Documents.DeleteDocument;
using TrainingPlatform.Application.Content.Documents.GetDocumentDownloadUrl;
using TrainingPlatform.Application.Content.Documents.GetDocumentVersionDownloadUrl;
using TrainingPlatform.Application.Content.Documents.GetDocumentVersions;
using TrainingPlatform.Application.Content.Documents.MarkLessonViewed;
using TrainingPlatform.Application.Content.Documents.ReplaceDocumentFile;
using TrainingPlatform.Application.Content.Documents.RequestDocumentUpload;
using TrainingPlatform.Application.Content.Documents.SaveVideoProgress;
using TrainingPlatform.Application.Content.Documents.UpdateDocumentLessonDetails;

namespace TrainingPlatform.Api.Endpoints.Documents;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var moduleDocuments = app.MapGroup("/api/v1/modules/{moduleId:guid}/documents")
            .WithTags("Documents")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        moduleDocuments.MapPost("/upload-url", async (
            Guid moduleId, RequestUploadRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new RequestDocumentUploadCommand(
                moduleId, request.Title, request.FileName, request.ContentType, request.SizeBytes);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        moduleDocuments.MapPost("/text-lessons", async (
            Guid moduleId, CreateTextLessonRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateTextLessonCommand(moduleId, request.Title, request.BodyText, request.Quote);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/documents/{result.Value}", new { id = result.Value })
                : CustomResults.Problem(result);
        });

        var documents = app.MapGroup("/api/v1/documents").WithTags("Documents").RequireAuthorization();

        documents.MapGet("/{id:guid}/download-url", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDocumentDownloadUrlQuery(id), ct);
            return result.IsSuccess ? Results.Ok(new { url = result.Value }) : CustomResults.Problem(result);
        });

        documents.MapPost("/{id:guid}/mark-viewed", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkLessonViewedCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        documents.MapPost("/{id:guid}/video-progress", async (
            Guid id, SaveVideoProgressRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SaveVideoProgressCommand(id, request.PositionSeconds), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        documents.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteDocumentCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        var versions = app.MapGroup("/api/v1/documents").WithTags("Documents").RequireAuthorization("RequireTrainerOrAdministrator");

        versions.MapPost("/{id:guid}/replace-url", async (Guid id, ReplaceUploadRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new ReplaceDocumentFileCommand(id, request.FileName, request.ContentType, request.SizeBytes);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        versions.MapGet("/{id:guid}/versions", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDocumentVersionsQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        versions.MapPut("/{id:guid}/lesson-details", async (Guid id, UpdateLessonDetailsRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateDocumentLessonDetailsCommand(
                id, request.Title, request.TranscriptText, request.SummaryText, request.KeyTakeaway,
                request.DurationMinutes, request.PageCount, request.Quote);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        });

        var documentVersions = app.MapGroup("/api/v1/document-versions")
            .WithTags("Documents")
            .RequireAuthorization("RequireTrainerOrAdministrator");

        documentVersions.MapGet("/{id:guid}/download-url", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDocumentVersionDownloadUrlQuery(id), ct);
            return result.IsSuccess ? Results.Ok(new { url = result.Value }) : CustomResults.Problem(result);
        });

        return app;
    }
}

public sealed record RequestUploadRequest(string Title, string FileName, string ContentType, long SizeBytes);

public sealed record ReplaceUploadRequest(string FileName, string ContentType, long SizeBytes);

public sealed record UpdateLessonDetailsRequest(
    string Title, string? TranscriptText, string? SummaryText, string? KeyTakeaway,
    int? DurationMinutes, int? PageCount, string? Quote);

public sealed record CreateTextLessonRequest(string Title, string BodyText, string? Quote);

public sealed record SaveVideoProgressRequest(int PositionSeconds);
