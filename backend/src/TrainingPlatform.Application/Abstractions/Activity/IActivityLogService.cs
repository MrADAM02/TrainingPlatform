using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Abstractions.Activity;

public interface IActivityLogService
{
    Task LogAsync(
        Guid userId,
        string action,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken);

    Task<Result<PaginatedList<ActivityLogItem>>> GetLogsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
