using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Certificates.GetCertificateById;
using TrainingPlatform.Application.Certificates.GetMyCertificates;

namespace TrainingPlatform.Api.Endpoints.Certificates;

public static class CertificateEndpoints
{
    public static IEndpointRouteBuilder MapCertificateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/certificates").WithTags("Certificates").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCertificatesQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCertificateByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : CustomResults.Problem(result);
        });

        return app;
    }
}
