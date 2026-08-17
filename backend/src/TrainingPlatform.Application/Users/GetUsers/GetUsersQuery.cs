using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Users.GetUsers;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20) : IQuery<PaginatedList<UserSummary>>;

public sealed class GetUsersQueryHandler(IIdentityService identityService)
    : IQueryHandler<GetUsersQuery, PaginatedList<UserSummary>>
{
    public Task<Result<PaginatedList<UserSummary>>> Handle(GetUsersQuery query, CancellationToken cancellationToken) =>
        identityService.GetUsersAsync(query.Page, query.PageSize, cancellationToken);
}
