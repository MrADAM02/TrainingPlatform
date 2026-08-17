using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Content.Documents.DeleteDocument;
using TrainingPlatform.Application.Content.Documents.GetDocumentDownloadUrl;
using TrainingPlatform.Application.Content.Documents.RequestDocumentUpload;

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

        var documents = app.MapGroup("/api/v1/documents").WithTags("Documents").RequireAuthorization();

        documents.MapGet("/{id:guid}/download-url", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDocumentDownloadUrlQuery(id), ct);
            return result.IsSuccess ? Results.Ok(new { url = result.Value }) : CustomResults.Problem(result);
        });

        documents.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteDocumentCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : CustomResults.Problem(result);
        })
        .RequireAuthorization("RequireTrainerOrAdministrator");

        return app;
    }
}

public sealed record RequestUploadRequest(string Title, string FileName, string ContentType, long SizeBytes);
