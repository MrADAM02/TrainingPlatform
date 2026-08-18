using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Search.SearchDocuments;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Api.Endpoints.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/search/documents", async (
            ISender sender,
            CancellationToken ct,
            string? keyword = null,
            Guid? courseId = null,
            DocumentType? contentType = null,
            Guid? trainerId = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new SearchDocumentsQuery(keyword, courseId, contentType, trainerId, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        })
        .WithTags("Search")
        .RequireAuthorization();

        return app;
    }
}
