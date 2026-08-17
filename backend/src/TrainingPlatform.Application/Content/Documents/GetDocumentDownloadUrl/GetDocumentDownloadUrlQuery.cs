using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Application.Abstractions.Storage;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Documents.GetDocumentDownloadUrl;

/// <summary>Authorization is checked before a signed URL is ever issued (REQ-CONT-05).</summary>
public sealed record GetDocumentDownloadUrlQuery(Guid DocumentId) : IQuery<string>;

public sealed class GetDocumentDownloadUrlQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    IActivityLogService activityLog,
    IUserContext currentUser) : IQueryHandler<GetDocumentDownloadUrlQuery, string>
{
    public async Task<Result<string>> Handle(GetDocumentDownloadUrlQuery query, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<string>(ContentErrors.DocumentNotFound(query.DocumentId));
        }

        var module = await dbContext.Modules.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == document.ModuleId, cancellationToken);
        var course = module is null
            ? null
            : await dbContext.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);

        if (course is null || !CourseAccess.CanView(course, currentUser))
        {
            return Result.Failure<string>(ContentErrors.CourseNotAccessible);
        }

        var url = await fileStorage.GetDownloadUrlAsync(document.StorageKey, cancellationToken);

        await activityLog.LogAsync(
            currentUser.UserId, ActivityActions.DocumentDownloaded, "Document", document.Id.ToString(), cancellationToken);

        return url;
    }
}
