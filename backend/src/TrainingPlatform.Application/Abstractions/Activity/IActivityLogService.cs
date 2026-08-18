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

    /// <summary>REQ-ADM-07: unpaginated export for reporting, optionally date-bounded and
    /// capped at a fixed row limit so a very wide date range can't return an unbounded
    /// response.</summary>
    Task<Result<IReadOnlyList<ActivityLogItem>>> ExportAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);
}

