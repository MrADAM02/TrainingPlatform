using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Users.SearchTrainees;

public sealed record SearchTraineesQuery(string? Keyword = null, int Limit = 20) : IQuery<IReadOnlyList<UserSummary>>;

public sealed class SearchTraineesQueryHandler(IIdentityService identityService)
    : IQueryHandler<SearchTraineesQuery, IReadOnlyList<UserSummary>>
{
    public Task<Result<IReadOnlyList<UserSummary>>> Handle(SearchTraineesQuery query, CancellationToken cancellationToken) =>
        identityService.SearchTraineesAsync(query.Keyword, query.Limit, cancellationToken);
}
