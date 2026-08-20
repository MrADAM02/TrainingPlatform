using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Certificates.Contracts;
using TrainingPlatform.Domain.Certificates;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Certificates.GetCertificateById;

public sealed record GetCertificateByIdQuery(Guid CertificateId) : IQuery<CertificateDetails>;

public sealed class GetCertificateByIdQueryHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : IQueryHandler<GetCertificateByIdQuery, CertificateDetails>
{
    public async Task<Result<CertificateDetails>> Handle(
        GetCertificateByIdQuery query, CancellationToken cancellationToken)
    {
        var certificate = await dbContext.Certificates.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == query.CertificateId, cancellationToken);

        if (certificate is null)
        {
            return Result.Failure<CertificateDetails>(CertificateErrors.NotFound(query.CertificateId));
        }

        var isOwner = certificate.UserId == currentUser.UserId;
        var isAdmin = currentUser.Roles.Contains(Roles.Administrator);

        if (!isOwner && !isAdmin)
        {
            return Result.Failure<CertificateDetails>(CertificateErrors.NotFound(query.CertificateId));
        }

        return new CertificateDetails(
            certificate.Id,
            certificate.CourseId,
            certificate.CourseTitle,
            certificate.RecipientFullName,
            certificate.CertificateNumber,
            certificate.IssuedAtUtc);
    }
}
