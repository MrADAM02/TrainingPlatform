using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Http;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Infrastructure.Database;

namespace TrainingPlatform.Infrastructure.Activity;

public sealed class ActivityLogService(ApplicationDbContext dbContext, IClientContext clientContext) : IActivityLogService
{
    public async Task LogAsync(
        Guid userId,
        string action,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken)
    {
        dbContext.ActivityLogs.Add(ActivityLog.Create(userId, action, entityType, entityId, clientContext.IpAddress));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<PaginatedList<ActivityLogItem>>> GetLogsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        // Left join so an entry survives even if the user it references has since been deleted
        // (an inner join would silently drop it, which defeats the point of an audit trail).
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(
                dbContext.Users.AsNoTracking(),
                log => log.UserId,
                user => user.Id,
                (log, users) => new { log, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, user) => new ActivityLogItem(
                    x.log.Id,
                    x.log.UserId,
                    user != null ? user.Email : null,
                    x.log.Action,
                    x.log.EntityType,
                    x.log.EntityId,
                    x.log.IpAddress,
                    x.log.TimestampUtc))
            .ToListAsync(cancellationToken);

        return new PaginatedList<ActivityLogItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    private const int MaxExportRows = 20_000;

    public async Task<Result<IReadOnlyList<ActivityLogItem>>> ExportAsync(
        DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = dbContext.ActivityLogs.AsNoTracking().AsQueryable();

        // The querystring model binder produces DateTimeKind.Unspecified; Npgsql refuses to
        // compare that against a timestamptz column. Treat caller-supplied bounds as UTC, which
        // is consistent with every other timestamp in this system.
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc <= toUtc);
        }

        IReadOnlyList<ActivityLogItem> items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(MaxExportRows)
            .GroupJoin(
                dbContext.Users.AsNoTracking(),
                log => log.UserId,
                user => user.Id,
                (log, users) => new { log, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, user) => new ActivityLogItem(
                    x.log.Id,
                    x.log.UserId,
                    user != null ? user.Email : null,
                    x.log.Action,
                    x.log.EntityType,
                    x.log.EntityId,
                    x.log.IpAddress,
                    x.log.TimestampUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
