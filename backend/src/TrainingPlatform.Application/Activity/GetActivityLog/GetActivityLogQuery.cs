using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Activity.GetActivityLog;

public sealed record GetActivityLogQuery(int Page = 1, int PageSize = 20) : IQuery<PaginatedList<ActivityLogItem>>;

public sealed class GetActivityLogQueryHandler(IActivityLogService activityLogService)
    : IQueryHandler<GetActivityLogQuery, PaginatedList<ActivityLogItem>>
{
    public Task<Result<PaginatedList<ActivityLogItem>>> Handle(GetActivityLogQuery query, CancellationToken cancellationToken) =>
        activityLogService.GetLogsAsync(query.Page, query.PageSize, cancellationToken);
}
