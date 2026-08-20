using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Certificates.Contracts;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Certificates.GetMyCertificates;

public sealed record GetMyCertificatesQuery : IQuery<IReadOnlyList<CertificateSummary>>;

public sealed class GetMyCertificatesQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetMyCertificatesQuery, IReadOnlyList<CertificateSummary>>
{
    public async Task<Result<IReadOnlyList<CertificateSummary>>> Handle(
        GetMyCertificatesQuery query, CancellationToken cancellationToken)
    {
        var certificates = await dbContext.Certificates.AsNoTracking()
            .Where(c => c.UserId == currentUser.UserId)
            .OrderByDescending(c => c.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        IReadOnlyList<CertificateSummary> items = certificates
            .Select(c => new CertificateSummary(c.Id, c.CourseId, c.CourseTitle, c.CertificateNumber, c.IssuedAtUtc))
            .ToList();

        return Result.Success(items);
    }
}
