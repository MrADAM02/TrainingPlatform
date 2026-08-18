using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Activity.ExportActivityLog;

public sealed record ExportActivityLogQuery(DateTime? From, DateTime? To) : IQuery<IReadOnlyList<ActivityLogItem>>;

public sealed class ExportActivityLogQueryHandler(IActivityLogService activityLogService)
    : IQueryHandler<ExportActivityLogQuery, IReadOnlyList<ActivityLogItem>>
{
    public Task<Result<IReadOnlyList<ActivityLogItem>>> Handle(ExportActivityLogQuery query, CancellationToken cancellationToken) =>
        activityLogService.ExportAsync(query.From, query.To, cancellationToken);
}
