using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserSummary>;

public sealed class GetUserByIdQueryHandler(IIdentityService identityService)
    : IQueryHandler<GetUserByIdQuery, UserSummary>
{
    public Task<Result<UserSummary>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) =>
        identityService.GetUserByIdAsync(query.UserId, cancellationToken);
}
